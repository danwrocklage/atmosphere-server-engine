using System.Net;
using System.Net.Sockets;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Transport;
using ACore.Transport;
using AGame.Frontend;
using AGame.Frontend.Dto;
using AGame.Frontend.Stages;
using AUtils.IoC;
using AUtils.Sil;

namespace Fb.Frontend.Bot;

[Log(Category = "[net] client")]
internal class NetworkClient : IDisposable
{
    private readonly ILogger<NetworkClient> mLogger;
    private readonly IContainer mContainer;
    private readonly NetworkConfiguration mConfiguration;
    private IClient? mNetworkClient;

    public NetworkClient(ILogger<NetworkClient> logger, IConfiguration configuration, 
        IContainer container)
    {
        mLogger = logger;
        mConfiguration = configuration.Get<NetworkConfiguration>(() => null!);
        mContainer = container;
        mNetworkClient = null;
    }

    public async Task Connect(CancellationToken cancellationToken = default)
    {
        mNetworkClient = mContainer.Resolve<TransportFactory>()
            .CreateClient(TransportType.UDP, "BotClient");
        
        var addresses = await Dns
            .GetHostAddressesAsync(mConfiguration.Host, AddressFamily.InterNetwork, cancellationToken);
        if (addresses.Length == 0)
            throw new ApplicationException("Failed to get IP address to frontend");

        var ip = addresses[0].ToString();

        await mNetworkClient.Connect(ip, mConfiguration.Port, cancellationToken);
    }

    public async Task RunPipeline(string gameToken, object startPipeline, CancellationToken cancellationToken = default)
    {
        if (startPipeline == null) throw new ArgumentNullException(nameof(startPipeline));
        if(mNetworkClient == null)
            throw new ArgumentNullException(nameof(mNetworkClient));

        if (string.IsNullOrEmpty(gameToken)) 
            throw new ArgumentNullException(nameof(gameToken));

        var df = new DiffieHellman();
        var message = new InitializeConnectionDto
        {
            Jwt = gameToken,
            ApplicationVersion = null,
            PublicKey = df.Export()
        };
        var buffer = new Memory<byte>(new byte[Sil.OutputSize(message)]);
        Sil.Serialize(message, buffer);
        await mNetworkClient.Send(buffer, cancellationToken);

        using var result = await mNetworkClient.Receive(cancellationToken);
        var (complete, type) = Sil.Deserialize(result);

        mLogger.Debug($"Receive: {type.FullName}");
        if (type == typeof(ConnectionResponseDto))
        {
            var response = (ConnectionResponseDto) complete;
            mLogger.Log($"{(response.IsError ? "ERROR: " : string.Empty)}{response.Message}",
                response.IsError ? LogLevel.Error : LogLevel.Warning);
            return;
        }

        if (type != typeof(CompleteConnectionDto))
        {
            return;
        }
        
        var serverKey = (CompleteConnectionDto) complete;
        df.Import(serverKey.PublicKey);
        df.IV = serverKey.IV;

        var pipeline = mContainer.Resolve<ConnectionPipeline>();
        pipeline.AddStage(new CompressionPipelineStage());
        pipeline.AddStage(new EncryptionPipelineStage(df));

        await pipeline.Run(startPipeline, mNetworkClient, Guid.Empty, cancellationToken);
    }

    public void Dispose()
    {
        mNetworkClient?.Dispose();
    }

    #region Utils

    [Configuration("frontend")]
    private class NetworkConfiguration
    {
        public string Host { get; set; }
        
        public int Port { get; set; }
    }

    #endregion
}