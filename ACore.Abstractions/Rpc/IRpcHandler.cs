namespace ACore.Abstractions.Rpc;

/// <summary>
/// Received message processor
/// </summary>
/// <typeparam name="T">Message type</typeparam>
public interface IRpcHandler<in T>
{
    /// <summary>
    /// Start processing message
    /// </summary>
    Task Handle(IRpcContext<T> context, CancellationToken token = default);
}