using System.Buffers;
using System.Diagnostics;
using System.Reflection;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Telemetry;
using ACore.Abstractions.Transport;
using AGame.Frontend.Dto;
using AGame.Frontend.Stages;
using AUtils.IoC;
using AUtils.Sil;

namespace AGame.Frontend;

/// <summary>
/// Main connection processing loop.
/// Read received message from client, process it and send result back
/// </summary>
internal class ConnectionPipeline
{
    #region Static handlers

    internal static Dictionary<Type, Type> PipelineHandlers { get; private set; } = new();
    
    internal static void Initialize(Assembly searchingAssembly = null)
    {
        PipelineHandlers = (searchingAssembly != null ? searchingAssembly.GetTypes() : Types.All)
            .Where(x => !x.IsAbstract && x.BaseType != typeof(object))
            .Where(x => x.GetParentLike(typeof(PipelineHandler<>)) != null)
            .ToDictionary(
                x => x.GetParentLike(typeof(PipelineHandler<>))?.GetGenericArguments().FirstOrDefault() ?? throw new InvalidOperationException(), 
                x => x);
    }

    #endregion
    
    private readonly ILogger<ConnectionPipeline> mLogger;
    private readonly IContainer mContainer;
    private readonly List<PipelineStage> mStages;
    private readonly ICellMetrics mMetrics;
    private readonly bool mIsDevelopment;
    
    public ConnectionPipeline(ILogger<ConnectionPipeline> logger, ICellEnvironment env, IContainer container, ICellMetrics metrics)
    {
        mLogger = logger;
        mContainer = container;
        mMetrics = metrics;
        mStages = new List<PipelineStage>();
        mIsDevelopment = env.Configuration == Cell.CONFIGURATION_DEVELOPMENT;
        
        CreateMetrics();
    }

    public void AddStage(PipelineStage stage)
    {
        if (stage == null) 
            throw new ArgumentNullException(nameof(stage));

        mStages.Add(stage);
    }

    public async Task Run(object firstMessage, IConnection connection, Guid entityId, CancellationToken token)
    {
        await SendData(connection, firstMessage, token);
        await Run(connection, entityId, token);
    }
    
    public async Task Run(IConnection connection, Guid entityId, CancellationToken token)
    {
        var context = new PipelineHandlerContext
        {
            EntityId = entityId,
            CancellationToken = token
        };
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
#if Production
                if(!Debugger.IsAttached)
                    cts.CancelAfter(3_000);
#endif
                var (input, messageType) = await ReceiveData(connection, cts.Token);
                if (messageType == null)
                {
                    mLogger.Warn("Receive empty message. Disconnect");
                    break;
                }

                if (!cts.TryReset())
                {
                    mLogger.Debug("Receiving data timeout. Disconnect");
                    break;
                }

                if (messageType == typeof(CloseConnectionDto))
                    break;

                if (!PipelineHandlers.TryGetValue(messageType, out var handlerType))
                {
                    mLogger.Error($"Executor for '{messageType.FullName}' wasn't found. Disconnect");
                    break;
                }

                var handler = (IPipelineHandler) mContainer.Resolve(handlerType);
                var result = await handler.Handle(input, context);

                if (result == null)
                    throw new InvalidOperationException("Pipeline handler execution result must not be null")
                    {
                        Data =
                        {
                            {"Executor", handlerType.FullName},
                            {"RemoteEndpoint", connection.RemoteEndpoint}
                        }
                    };

                await SendData(connection, result, cts.Token);

                if (result is CloseConnectionDto)
                    break;
            }
        }
        catch (Exception e)
        {
            if (e is TaskCanceledException or OperationCanceledException)
                mLogger.Debug("Connection was cancelled");
            else
                mLogger.Error("Connection fail", e);
        }
        finally
        {
            await context.OnCloseInvoke();
            mLogger.Debug("Connection was closed");
        }
    }
    
    private async Task SendData(IConnection connection, object result, CancellationToken token)
    {
        var memory = MemoryPool<byte>.Shared.Rent(Sil.OutputSize(result));
        Sil.Serialize(result, memory.Memory);
        var buffer = new Packet(memory);

        for (byte i = 0; i < mStages.Count; i++)
            buffer = mStages[i].InternalProcess(buffer, PipelineDirection.Sending);

        await connection.Send(buffer, token);
    }

    private async Task<(object, Type)> ReceiveData(IConnection connection, CancellationToken token)
    {
        var packet = await connection.Receive(token);
        if (packet.Data.IsEmpty)
            return default;

        for (var i = mStages.Count - 1; i >= 0; i--)
            packet = mStages[i].InternalProcess(packet, PipelineDirection.Receiving);

        var result = Sil.Deserialize(packet.Data);
        packet.Dispose();
        return result;
    }
    
    #region Utils

    private void CreateMetrics()
    {
        mMetrics.Create("connection_bytes_send_total", MetricsType.Counter, "Total of sent bytes to remotes", "entity");
        mMetrics.Create("connection_packets_send_total", MetricsType.Counter, "Total of sent packets to remotes",
            "entity");

        mMetrics.Create("connection_bytes_receive_total", MetricsType.Counter, "Total of received bytes from remotes",
            "entity");
        mMetrics.Create("connection_packets_receive_total", MetricsType.Counter,
            "Total of received packets from remotes", "entity");
    }

    #endregion
}