using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Telemetry;
using ACore.Abstractions.Transport;

namespace ACore.Transport.Udp;

/// <inheritdoc cref="IServer"/>
[Log(Category = "udp.server")]
internal class UdpServer : IServer
{
    private readonly ICellMetrics mCellMetrics;
    private readonly ILogger<UdpServer> mLogger;
    private readonly ICellEnvironment mEnvironment;

    private readonly ConcurrentDictionary<EndPoint, UdpServerConnection> mConnections;
    private readonly ConcurrentQueue<(EndPoint, Packet)> mPacketsForSend;
    private readonly Timer mDisconnectTimer;
    private Socket mSocket;
    private readonly int mPort;
    private readonly int mBufferSize;
    private readonly TimeSpan mDisconnectionTime;
    private CancellationTokenSource mCancellationTokenSource;

    public UdpServer(ILogger<UdpServer> logger, IConfiguration configuration, ICellMetrics cellMetrics,
        ICellEnvironment environment)
    {
        mLogger = logger;
        mCellMetrics = cellMetrics;
        mEnvironment = environment;
        mConnections = new ConcurrentDictionary<EndPoint, UdpServerConnection>();
        mPacketsForSend = new ConcurrentQueue<(EndPoint, Packet)>();
        var config = configuration.Get(() => TransportServerConfig.Default);
        mPort = config.InPort;
        mBufferSize = config.BufferSize;

        mDisconnectionTime = TimeSpan.FromSeconds(config.Timeout);
        mDisconnectTimer = new Timer(_ => RemoveByTimeout(), null, mDisconnectionTime, mDisconnectionTime);

        CreateMetrics();
    }

    public event NewServerConnection NewConnection;

    /// <inheritdoc />
    public void Initialize()
    {
        mLogger.Info("UDP server initializing");
        mSocket = Sockets.CreateUdp(mBufferSize);
        mSocket.Bind(new IPEndPoint(IPAddress.Any, mPort));
    }

    /// <inheritdoc />
    /// <remarks>Don't forget about <see cref="RunNonBlocking"/></remarks>
    [SuppressMessage("ReSharper", "RedundantAssignment")]
    public async Task Run(CancellationToken token)
    {
        mCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        mLogger.Info($"Start listening on udp://{mSocket.LocalEndPoint}");

        ThreadPool.QueueUserWorkItem(_ => _ = SendProcess());
        await ReceiveProcess();

        TryDisconnectAll();

        mLogger.Info("Stop listening");
    }

    /// <inheritdoc />
    public void Stop()
    {
        mLogger.Info("Stopping server");
        mCancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// Non blocking version of <see cref="Run"/> for unit tests
    /// </summary>
    [SuppressMessage("ReSharper", "RedundantAssignment")]
    internal void RunNonBlocking(CancellationToken token)
    {
        mCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        mLogger.Info($"Start listening on udp://{mSocket.LocalEndPoint}");
        ThreadPool.QueueUserWorkItem(_ => _ = ReceiveProcess());
        ThreadPool.QueueUserWorkItem(_ => _ = SendProcess());
    }

    #region Receiving

    private async Task ReceiveProcess()
    {
        EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
        var isNewConnection = false;

        while (!mCancellationTokenSource.Token.IsCancellationRequested)
        {
            while (mSocket.Available <= 0)
            {
                if (mCancellationTokenSource.Token.IsCancellationRequested)
                    break;
            }

            if (mCancellationTokenSource.Token.IsCancellationRequested)
                break;

            var buffer = MemoryPool<byte>.Shared.Rent(mSocket.Available);
            SocketReceiveFromResult read;

            try
            {
                read = await mSocket.ReceiveFromAsync(buffer.Memory, SocketFlags.None, remoteIp,
                    mCancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                if(e is not SocketException)
                    mLogger.Error("Receive message fail", e);
                buffer.Dispose();
                continue;
            }

            if (!mConnections.ContainsKey(read.RemoteEndPoint))
                isNewConnection = AddNewConnection(read);

            mLogger.Debug($"Got {read.ReceivedBytes.ToString()} bytes from {read.RemoteEndPoint}");

            var connection = mConnections[read.RemoteEndPoint];

            if (read.ReceivedBytes <= 0)
                return;

            connection.ReceiveInternal(new Packet(buffer).Slice(0, read.ReceivedBytes));

            mCellMetrics.Get("cell_udp_server_bytes_receive_total").Inc(read.ReceivedBytes, mEnvironment.Role);
            mCellMetrics.Get("cell_udp_server_packets_receive_total").Inc(mEnvironment.Role);

            if (isNewConnection && NewConnection != null)
            {
                _ = NewConnection(connection, mCancellationTokenSource.Token)
                    .ContinueWith((t, l) =>
                    {
                        var logger = (ILogger<UdpServer>) l;
                        var ex = t.Exception;
                        if (ex?.InnerExceptions.First() is not TaskCanceledException)
                            logger.Error("Client connection fail", t.Exception);
                        else
                            logger.Debug("Client connection has been cancelled");
                    }, mLogger, TaskContinuationOptions.OnlyOnFaulted)
                    .ContinueWith((_, c) => ((IConnection) c).Disconnect(), connection, mCancellationTokenSource.Token)
                    .ConfigureAwait(false);
                isNewConnection = false;
            }
        }
    }

    /// <summary>
    /// Register new connection
    /// </summary>
    private bool AddNewConnection(SocketReceiveFromResult e)
    {
        mLogger.Debug($"New connection {e.RemoteEndPoint}");
        var client = new UdpServerConnection(this, e.RemoteEndPoint);
        if (!mConnections.TryAdd(e.RemoteEndPoint, client))
        {
            mLogger.Warn($"Can't add connection {e.RemoteEndPoint}");
            return false;
        }

        mCellMetrics.Get("cell_udp_server_connections_total").Inc(mEnvironment.Role);
        return true;
    }

    #endregion

    #region Senging

    private async Task SendProcess()
    {
        while (!mCancellationTokenSource.Token.IsCancellationRequested)
        {
            if (mPacketsForSend.IsEmpty || !mPacketsForSend.TryDequeue(out var queueItem))
                continue;

            mLogger.Debug($"Send {queueItem.Item2.Data.Length.ToString()} bytes to {queueItem.Item1}");
            var sent = await mSocket.SendToAsync(queueItem.Item2, SocketFlags.None, queueItem.Item1,
                mCancellationTokenSource.Token);
            queueItem.Item2.Dispose();

            mCellMetrics.Get("cell_udp_server_bytes_send_total").Inc(sent, mEnvironment.Role);
            mCellMetrics.Get("cell_udp_server_packets_send_total").Inc(mEnvironment.Role);
        }
    }

    internal void EnqueueToSend(EndPoint endPoint, Packet packet) => mPacketsForSend.Enqueue((endPoint, packet));

    #endregion

    #region Disconnection

    private void TryDisconnectAll()
    {
        var endpoints = mConnections.Keys.ToArray();
        foreach (var endpoint in endpoints)
            TryDisconnect(endpoint);
    }

    private void RemoveByTimeout()
    {
        var time = DateTime.UtcNow - mDisconnectionTime;
        var count = 0;
        foreach (var (endPoint, udpServerConnection) in mConnections)
        {
            if (udpServerConnection.LastUpdated < time)
                continue;

            count++;
            TryDisconnect(endPoint);
        }

        if (count == 0)
            return;

        mCellMetrics.Get("cell_udp_server_disconnected_by_timeout_total").Inc(count, mEnvironment.Role);
        mLogger.Debug($"{count.ToString()} disconnected by timeout");
    }

    internal void TryDisconnect(EndPoint endPoint)
    {
        if (mConnections.TryRemove(endPoint, out _))
            mCellMetrics.Get("cell_udp_server_connections_total").Dec(mEnvironment.Role);
        else
            mLogger.Warn($"Can't remove dead connection {endPoint}");
    }

    #endregion

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        mLogger.Info("Dispose");
        TryDisconnectAll();
        await mDisconnectTimer.DisposeAsync();
        if (mSocket != null)
            await Task.Run(() =>
            {
                mSocket?.Shutdown(SocketShutdown.Both);
                mSocket?.Close(2);
                mSocket?.Dispose();
                mSocket = null;
                mCellMetrics.Get("cell_udp_server_connections_total")
                    .Post(0, MetricOperationType.SetValue, mEnvironment.Role);
            });
    }

    #region Utils

    private void CreateMetrics()
    {
        mCellMetrics.Create("cell_udp_server_bytes_send_total", MetricsType.Counter, "Total of sent bytes to remotes",
            "cell_role");
        mCellMetrics.Create("cell_udp_server_packets_send_total", MetricsType.Counter,
            "Total of sent packets to remotes", "cell_role");

        mCellMetrics.Create("cell_udp_server_bytes_receive_total", MetricsType.Counter,
            "Total of received bytes from remotes", "cell_role");
        mCellMetrics.Create("cell_udp_server_packets_receive_total", MetricsType.Counter,
            "Total of received packets from remotes", "cell_role");

        mCellMetrics.Create("cell_udp_server_connections_total", MetricsType.Gauge, "Total of current connections",
            "cell_role");
        mCellMetrics.Create("cell_udp_server_disconnected_by_timeout_total", MetricsType.Counter,
            "Total of disconnected by timeout connections", "cell_role");
    }

    #endregion
}