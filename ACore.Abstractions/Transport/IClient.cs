namespace ACore.Abstractions.Transport;

/// <summary>
/// Low level client connection interface
/// </summary>
public interface IClient : IConnection, IDisposable
{
    /// <summary>
    /// Connect to remote host
    /// </summary>
    Task Connect(string host, int port, CancellationToken token = default);
}