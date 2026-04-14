using System.Collections.Concurrent;
using System.Net;
using ACore.Abstractions.Transport;

namespace ACore.Transport.Udp;

/// <summary>
/// Manage sending and receiving for client
/// </summary>
internal class UdpServerConnection : IConnection
{
    private static ushort sClientIdCurrent;

    private readonly ConcurrentQueue<Packet> mReceivedPackets;
    private readonly UdpServer mServer;

    public UdpServerConnection(UdpServer server, EndPoint endPoint)
    {
        mServer = server;
        RemoteEndpoint = endPoint;
        mReceivedPackets = new ConcurrentQueue<Packet>();
        LastUpdated = DateTime.UtcNow;

        ClientId = sClientIdCurrent;
        sClientIdCurrent++;
    }

    /// <summary>
    /// Timestamp of last network event (send/receive)
    /// </summary>
    internal DateTime LastUpdated { get; private set; }

    /// <summary>
    /// Unique client id
    /// </summary>
    internal ushort ClientId { get; }

    /// <summary>
    /// Store packed, which was read from listener
    /// </summary>
    internal void ReceiveInternal(Packet packet)
    {
        mReceivedPackets.Enqueue(packet);
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Enqueue new message to send
    /// </summary>
    public Task Send(Packet packet, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        
        mServer.EnqueueToSend(RemoteEndpoint, packet);
        LastUpdated = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get received packet. This call blocks until receive queue will have any message
    /// </summary>
    public Task<Packet> Receive(CancellationToken token = default)
    {
        while (mReceivedPackets.IsEmpty)
        {
            if (token.IsCancellationRequested)
                break;
        }
        
        token.ThrowIfCancellationRequested();

        var packet = mReceivedPackets.TryDequeue(out var data) ? data : Packet.Empty;
        return Task.FromResult(packet);
    }

    /// <summary>
    /// Connected client endpoint
    /// </summary>
    public EndPoint RemoteEndpoint { get; }

    public void Disconnect()
    {
        // Self disconnect
        mServer.TryDisconnect(RemoteEndpoint);
    }

    public override string ToString() => $"UDP Client {ClientId.ToString()} ({RemoteEndpoint})";
}