namespace ACore.Abstractions.Transport;

public delegate Task NewServerConnection(IConnection connection, CancellationToken token);

/// <summary>
/// Low level host for network transport connections
/// </summary>
public interface IServer : IRunnable, IInitializable, IAsyncDisposable
{
    /// <summary>
    /// Event for new connected clients
    /// </summary>
    event NewServerConnection NewConnection;

    /// <summary>
    /// Stop accepting new client
    /// </summary>
    void Stop();
}