namespace ACore.Abstractions.Transport;

/// <summary>
/// Low level interface for communication
/// </summary>
public interface IConnection
{
    /// <summary>
    /// Send message
    /// </summary>
    Task Send(Packet packet, CancellationToken token = default);

    /// <summary>
    /// Receive message. This is blocking call
    /// </summary>
    /// <param name="token"></param>
    Task<Packet> Receive(CancellationToken token = default);

    /// <summary>
    /// Remote endpoint information
    /// </summary>
    System.Net.EndPoint RemoteEndpoint { get; }

    /// <summary>
    /// Disconnect from remote
    /// </summary>
    void Disconnect();
}