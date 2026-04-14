using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Telemetry;
using ACore.Abstractions.Transport;

namespace ACore.Transport.Tcp;

/// <inheritdoc />
internal class TcpClient : IClient
{
    private int mTotalBytesSend = 0;
    private int mTotalBytesReceived = 0;
    
    private readonly ILogger<TcpClient> mLogger;
    private readonly ICellMetrics mMetrics;
    private NetworkStream mNetworkStream;
    private readonly System.Net.Sockets.TcpClient mTcpClient;
    private readonly Memory<byte> mInputSizeBuffer;
    private readonly Memory<byte> mOutputSizeBuffer;

    public TcpClient(ILogger<TcpClient> logger, ICellMetrics metrics)
    {
        mLogger = logger;
        mMetrics = metrics;
        mTcpClient = new System.Net.Sockets.TcpClient();
        mInputSizeBuffer = new Memory<byte>(new byte[4]);
        mOutputSizeBuffer = new Memory<byte>(new byte[4]);
        
        CreateMetrics();
    }
    
    internal string ClientName { get; set; }

    /// <inheritdoc />
    public EndPoint RemoteEndpoint { get; private set; }

    /// <inheritdoc />
    public async Task Connect(string host, int port, CancellationToken token = default)
    {
        await mTcpClient.ConnectAsync(host, port, token);
        
        mNetworkStream = mTcpClient.GetStream();
        RemoteEndpoint = mTcpClient.Client.RemoteEndPoint;
        
        mLogger.Info($"Connected to {RemoteEndpoint}");
    }
    
    public async Task Send(Packet packet, CancellationToken token = default)
    {
        if(packet.Data.IsEmpty)
            return;

        token.ThrowIfCancellationRequested();

        mTotalBytesSend += packet.Data.Length;
        
        Unsafe.As<byte, int>(ref mInputSizeBuffer.Span[0]) = packet.Data.Length;
        await mNetworkStream.WriteAsync(mInputSizeBuffer, token);
        await mNetworkStream.WriteAsync(packet.Data, token);
        
        mMetrics.Get("cell_tcp_client_bytes_send_total").Inc(packet.Data.Length, ClientName);
        mMetrics.Get("cell_tcp_client_packets_send_total").Inc(ClientName);
    }

    public async Task<Packet> Receive(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        
        var read = await mNetworkStream.ReadAsync(mOutputSizeBuffer, token);
        if (read != 4)
            return Packet.Empty;

        var bufferLen = BitConverter.ToInt32(mOutputSizeBuffer.Span);

        var buffer = MemoryPool<byte>.Shared.Rent(bufferLen);
        read = await mNetworkStream.ReadAsync(buffer.Memory, token);
        
        mTotalBytesReceived += read;

        mMetrics.Get("cell_tcp_client_bytes_receive_total").Inc(read, ClientName);
        mMetrics.Get("cell_tcp_client_packets_receive_total").Inc(ClientName);
        
        return new Packet(buffer).Slice(0, read);
    }
    
    /// <inheritdoc />
    public void Disconnect()
    {
        mTcpClient.Close();
        mLogger.Info($"Disconnected from {RemoteEndpoint}");
    }

    /// <inheritdoc />
    public override string ToString() => $"TCP Client to ({RemoteEndpoint}) send: {mTotalBytesSend}, received: {mTotalBytesReceived}";
        
    /// <inheritdoc />
    public void Dispose()
    {
        Disconnect();
        mTcpClient.Dispose();
    }

    private void CreateMetrics()
    {
        mMetrics.Create("cell_tcp_client_bytes_send_total", MetricsType.Counter, "Total of sent bytes to remote",
            "client");
        mMetrics.Create("cell_tcp_client_packets_send_total", MetricsType.Counter, "Total of sent packets to remote",
            "client");

        mMetrics.Create("cell_tcp_client_bytes_receive_total", MetricsType.Counter,
            "Total of received bytes from remote", "client");
        mMetrics.Create("cell_tcp_client_packets_receive_total", MetricsType.Counter,
            "Total of received packets from remote", "client");
    }
}