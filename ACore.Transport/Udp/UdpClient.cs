using System.Buffers;
using System.Net;
using System.Net.Sockets;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Telemetry;
using ACore.Abstractions.Transport;

// ReSharper disable ClassNeverInstantiated.Global

namespace ACore.Transport.Udp;

/// <inheritdoc />
internal class UdpClient : IClient
{
    private readonly ILogger<UdpClient> mLogger;
    private Socket mSocket;
    private EndPoint mEndPoint;
    private readonly ICellMetrics mMetrics;

    public UdpClient(ILogger<UdpClient> logger, ICellMetrics metrics)
    {
        mLogger = logger;
        mMetrics = metrics;

        CreateMetrics();
    }

    internal string ClientName { get; set; }

    /// <inheritdoc />
    public EndPoint RemoteEndpoint => mEndPoint;

    /// <inheritdoc />
    public Task Connect(string host, int port, CancellationToken token = default)
    {
        mSocket = Sockets.CreateUdp(2048);
        mEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
        mLogger.Info($"Connected to {mEndPoint}");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        mSocket?.Shutdown(SocketShutdown.Both);
        mSocket?.Close(2);
        mSocket?.Dispose();
        mSocket = null;
        mLogger.Info($"Disconnected from {mEndPoint}");
    }

    /// <inheritdoc />
    public async Task Send(Packet packet, CancellationToken token = default)
    {
        if(packet.Data.IsEmpty)
            return;

        token.ThrowIfCancellationRequested();

        await mSocket.SendToAsync(packet, SocketFlags.None, RemoteEndpoint, token);
        mMetrics.Get("cell_udp_client_bytes_send_total").Inc(packet.Data.Length, ClientName);
        mMetrics.Get("cell_udp_client_packets_send_total").Inc(ClientName);
    }

    /// <inheritdoc />
    public async Task<Packet> Receive(CancellationToken token = default)
    {
        while (mSocket.Available == 0)
        {
            if(token.IsCancellationRequested)
                break;
        }

        token.ThrowIfCancellationRequested();
            
        var buffer = MemoryPool<byte>.Shared.Rent(mSocket.Available);
        var read = await mSocket.ReceiveFromAsync(buffer.Memory, SocketFlags.None, mEndPoint, token);

        mMetrics.Get("cell_udp_client_bytes_receive_total").Inc(read.ReceivedBytes, ClientName);
        mMetrics.Get("cell_udp_client_packets_receive_total").Inc(ClientName);

        return new Packet(buffer).Slice(0, read.ReceivedBytes);
    }
        
    /// <inheritdoc />
    public override string ToString() => $"UDP Client to ({RemoteEndpoint})";
        
    /// <inheritdoc />
    public void Dispose()
    {
        Disconnect();
    }

    private void CreateMetrics()
    {
        mMetrics.Create("cell_udp_client_bytes_send_total", MetricsType.Counter, "Total of sent bytes to remote",
            "client");
        mMetrics.Create("cell_udp_client_packets_send_total", MetricsType.Counter, "Total of sent packets to remote",
            "client");

        mMetrics.Create("cell_udp_client_bytes_receive_total", MetricsType.Counter,
            "Total of received bytes from remote", "client");
        mMetrics.Create("cell_udp_client_packets_receive_total", MetricsType.Counter,
            "Total of received packets from remote", "client");
    }
}