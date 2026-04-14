using System.Net;
using System.Net.Sockets;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Telemetry;
using ACore.Abstractions.Transport;

namespace ACore.Transport.Tcp;

[Log(Category = "tcp.server")]
internal class TcpServer : IServer
{
    private readonly ILogger<TcpServer> mLogger;
    private readonly ICellMetrics mCellMetrics;
    private TcpListener mListener;
    private CancellationTokenSource mCancellationTokenSource;
    private readonly int mPort;
    private readonly int mBufferSize;
    private readonly int mTimeout;

    public TcpServer(ILogger<TcpServer> logger, IConfiguration configuration, ICellMetrics cellMetrics)
    {
        mLogger = logger;
        mCellMetrics = cellMetrics;
        
        var config = configuration.Get(() => TransportServerConfig.Default);
        mPort = config.InPort;
        mTimeout = config.Timeout;
        mBufferSize = config.BufferSize;
        
        CreateMetrics();
    }
    
    public event NewServerConnection NewConnection;
    
    public void Stop()
    {
        mLogger.Info("Stopping server");
        mCancellationTokenSource?.Cancel();
    }

    public async Task Run(CancellationToken token)
    {
        mCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        mCancellationTokenSource.Token.Register(() => mListener.Stop());
        mListener.Start();
        mLogger.Info($"Start listening on udp://{mListener.Server.LocalEndPoint}");

        while (!mCancellationTokenSource.Token.IsCancellationRequested)
        {
            var client = await mListener.AcceptTcpClientAsync(mCancellationTokenSource.Token);
            client.SendTimeout = mTimeout;
            client.ReceiveTimeout = mTimeout;
            client.SendBufferSize = mBufferSize;
            client.ReceiveBufferSize = mBufferSize;
            
            var connection = new TcpServerConnection(client);
            _ = NewConnection?.Invoke(connection, mCancellationTokenSource.Token)
                .ContinueWith((t, l) =>
                {
                    var logger = (ILogger<TcpServer>) l;
                    var ex = t.Exception;
                    if (ex?.InnerExceptions.First() is not TaskCanceledException)
                        logger.Error("Client connection fail", t.Exception);
                    else
                        logger.Debug("Client connection has been cancelled");
                }, mLogger, TaskContinuationOptions.OnlyOnFaulted)
                .ContinueWith((_, c) => ((IConnection) c).Disconnect(), connection, mCancellationTokenSource.Token)
                .ConfigureAwait(false);
        }
    }

    public void Initialize()
    {
        mLogger.Info("TCP server initializing");
        mListener = new TcpListener(IPAddress.Any, mPort);
        mListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, mBufferSize);
        mListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, mBufferSize);
    }

    public ValueTask DisposeAsync()
    {
        mListener = null;
        return ValueTask.CompletedTask;
    }
    
    #region Utils

    private void CreateMetrics()
    {
        mCellMetrics.Create("cell_tcp_server_bytes_send_total", MetricsType.Counter, "Total of sent bytes to remotes",
            "cell_role");
        mCellMetrics.Create("cell_tcp_server_packets_send_total", MetricsType.Counter,
            "Total of sent packets to remotes", "cell_role");

        mCellMetrics.Create("cell_tcp_server_bytes_receive_total", MetricsType.Counter,
            "Total of received bytes from remotes", "cell_role");
        mCellMetrics.Create("cell_tcp_server_packets_receive_total", MetricsType.Counter,
            "Total of received packets from remotes", "cell_role");

        mCellMetrics.Create("cell_tcp_server_connections_total", MetricsType.Gauge, "Total of current connections",
            "cell_role");
        mCellMetrics.Create("cell_tcp_server_disconnected_by_timeout_total", MetricsType.Counter,
            "Total of disconnected by timeout connections", "cell_role");
    }

    #endregion
}