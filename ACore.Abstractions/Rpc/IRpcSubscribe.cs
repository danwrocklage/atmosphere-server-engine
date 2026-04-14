namespace ACore.Abstractions.Rpc;

/// <summary>
/// Interface for receiving messages from topics
/// </summary>
public interface IRpcSubscribe
{
    /// <summary>
    /// Resolve <see cref="IRpcHandler{T}"/> and start handle messages from topic as class <see cref="T"/> name
    /// </summary>
    /// <typeparam name="T">Message type</typeparam>
    void Subscribe<T>();
    
    /// <summary>
    /// Resolve <see cref="IRpcHandler{T}"/> and start handle messages from multiply topics
    /// </summary>
    /// <typeparam name="T">Message type</typeparam>
    void Subscribe<T>(params string[] topics);
    
    /// <summary>
    /// Start handle messages from topic as class <see cref="T"/> name using <paramref name="handler"/>
    /// </summary>
    /// <typeparam name="T">Message type</typeparam>
    void Subscribe<T>(IRpcHandler<T> handler);
    
    /// <summary>
    /// Start handle messages from <paramref name="topic"/> using <paramref name="handler"/>
    /// </summary>
    /// <typeparam name="T">Message type</typeparam>
    void Subscribe<T>(string topic, IRpcHandler<T> handler);
}