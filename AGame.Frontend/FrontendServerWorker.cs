using System.Buffers;
using System.Diagnostics;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Telemetry;
using ACore.Abstractions.Transport;
using ACore.Abstractions.Worker;
using ACore.Transport;
using AGame.Core.Account;
using AGame.Core.ClientApp;
using AGame.Core.Identity;
using AGame.Frontend.Dto;
using AGame.Frontend.Queue;
using AGame.Frontend.Stages;
using AUtils.IoC;
using AUtils.Sil;

namespace AGame.Frontend;

/// <summary>
/// Main server listening worker.
/// </summary>
[Worker("front-server")]
[Log(Category = "Frontend")]
internal class FrontendServerWorker : IRunnable
{
    private readonly IConnectionAccounter mConnectionAccounter;
    private readonly ConnectionEnableService mConnectionEnableService;
    private readonly FrontendServerConfig mServerConfig;
    private readonly TransportFactory mTransportFactory;
    private readonly IContainer mContainer;
    private readonly IClientBuildService mClientBuildService;
    private readonly ICellMetrics mMetrics;
    private readonly IAccountAccessService mAccountAccessService;
    private readonly IJwtService mJwtService;
    private readonly ILogger<FrontendServerWorker> mLogger;

    public FrontendServerWorker(TransportFactory transportFactory, IContainer container, 
        ILogger<FrontendServerWorker> logger, IClientBuildService clientBuildService, ICellMetrics metrics,
        IJwtService jwtService, IAccountAccessService accountAccessService, IConfiguration configuration, 
        ConnectionEnableService connectionEnableService, IConnectionAccounter connectionAccounter)
    {
        mServerConfig = configuration.Get(() => FrontendServerConfig.Default);
        mTransportFactory = transportFactory;
        mContainer = container;
        mLogger = logger;
        mClientBuildService = clientBuildService;
        mMetrics = metrics;
        mJwtService = jwtService;
        mAccountAccessService = accountAccessService;
        mConnectionEnableService = connectionEnableService;
        mConnectionAccounter = connectionAccounter;

        CreateMetrics();
    }

    public async Task Run(CancellationToken token)
    {
        await using var server = mTransportFactory.CreateServer(mServerConfig.TransportType);
        server.NewConnection += ServerOnNewConnection;
        server.Initialize();
        await server.Run(token);
    }

    /// <summary>
    /// Main player connection initialization
    /// </summary>
    private async Task ServerOnNewConnection(IConnection connection, CancellationToken token)
    {
        if(!mConnectionEnableService.IsEnable)
            return;

        var preparingCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        if(!Debugger.IsAttached)
            preparingCts.CancelAfter(mServerConfig.PrepareTime);

        if (!mConnectionAccounter.IsAvailable)
        {
            await SendError(connection, ClientResponseCodes.MaxConnectionsExceeded, preparingCts.Token);
            return;
        }
        
        await using var holder = await mConnectionAccounter.Reserve();
        var initDto = await TryGetInitDto(connection, preparingCts.Token); 
        if (initDto == null)
            return;

        if (!await mClientBuildService.IsVersionSupported(initDto.ApplicationVersion))
        {
            await SendError(connection, ClientResponseCodes.AppVersionNotSupported, preparingCts.Token);
            mLogger.Debug($"Client versions mismatch (not supported) ({connection.RemoteEndpoint})");
            return;
        }

        var (entityId, type, grandType) = mJwtService.GetEntityFromJwt(initDto.Jwt);
        if (grandType != GrandTypes.Client)
        {
            await SendError(connection, ClientResponseCodes.Unauthorized, preparingCts.Token);
            mLogger.Debug($"Client JWT grand type is mismatch ({connection.RemoteEndpoint}) ({entityId})");
            return;
        }
        if (type != typeof(AccountEntity).FullName)
        {
            await SendError(connection, ClientResponseCodes.Unauthorized, preparingCts.Token);
            mLogger.Debug($"Client JWT entity type is mismatch ({connection.RemoteEndpoint}) ({entityId})");
            return;
        }

        if (await mAccountAccessService.CanPlay(entityId) != true)
        {
            await SendError(connection, ClientResponseCodes.Unauthorized, preparingCts.Token);
            mLogger.Debug($"Client account isn't active or doesn't exist ({connection.RemoteEndpoint}) ({entityId})");
            return;
        }

        if (preparingCts.IsCancellationRequested)
        {
            await SendError(connection, ClientResponseCodes.InvalidConnectionData, token);
            mLogger.Debug($"Client connection preparation time out ({connection.RemoteEndpoint}) ({entityId})");
            return;
        }
        
        /*if (!mConnectionAccounter.IsWaiting(entityId))
        {
            await SendError(connection, ClientResponseCodes.InvalidConnectionData, token);
            mLogger.Debug($"Client is unwaitable ({connection.RemoteEndpoint}) ({entityId})");
            return;
        }*/

        if (initDto.PublicKey == null || initDto.PublicKey.Length == 0)
        {
            await SendError(connection, ClientResponseCodes.InvalidConnectionData, token);
            mLogger.Debug($"Client public key is invalid ({connection.RemoteEndpoint}) ({entityId})");
            return;
        }

        using var keyExchanger = new DiffieHellman();
        try
        {
            keyExchanger.Import(initDto.PublicKey);
        }
        catch (Exception e)
        {
            await SendError(connection, ClientResponseCodes.InvalidConnectionData, token);
            mLogger.Debug($"Client public key is invalid ({connection.RemoteEndpoint}) ({entityId})", e);
            return;
        }

        await SendMessage(connection, new CompleteConnectionDto
        {
            PublicKey = keyExchanger.Export(),
            IV = keyExchanger.IV
        }, token);

        var pipeline = mContainer.Resolve<ConnectionPipeline>();
        
        if(mServerConfig.Compression)
            pipeline.AddStage(new CompressionPipelineStage());
    
        if(mServerConfig.Encryption)
            pipeline.AddStage(new EncryptionPipelineStage(keyExchanger));
        
        await pipeline.Run(connection, entityId, token);
    }

    private static async Task SendMessage<T>(IConnection connection, T message, CancellationToken token)
    {
        var memory = MemoryPool<byte>.Shared.Rent(Sil.OutputSize(message));
        Sil.Serialize(message, memory.Memory);
        await connection.Send(new Packet(memory), token);
    }

    private static Task SendError(IConnection connection, string message, CancellationToken token = default)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (message == null) throw new ArgumentNullException(nameof(message));

        return SendMessage(connection,new ConnectionResponseDto
        {
            IsError = true,
            Message = message
        }, token);
    }

    private async Task<InitializeConnectionDto> TryGetInitDto(IConnection connection, CancellationToken token)
    {
        using var initDtoBuffer = await connection.Receive(token);
        try
        {
            var (initDtoObject, initDtoType) = Sil.Deserialize(initDtoBuffer.Data);
            if (initDtoType == typeof(InitializeConnectionDto) && 
                initDtoObject is InitializeConnectionDto initDto)
                return initDto;

            mLogger.Warn($"Failed to deserialize initial client message from {connection.RemoteEndpoint}");
        }
        catch (Exception e)
        {
            mLogger.Error($"Failed to deserialize initial client message from {connection.RemoteEndpoint}", e);
            mMetrics.Get("frontend_connection_deserialization_error").Inc();
        }

        return null;
    }

    private void CreateMetrics()
    {
        mMetrics.Create("frontend_connection_deserialization_error", MetricsType.Counter);
    }
}