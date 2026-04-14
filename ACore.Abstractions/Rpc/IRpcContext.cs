namespace ACore.Abstractions.Rpc;

/// <summary>
/// <see cref="IRpcHandler{T}"/> processing context for single receive
/// </summary>
/// <typeparam name="T">Message type</typeparam>
public interface IRpcContext<out T>
{
    /// <summary>
    /// Received message
    /// </summary>
    T Message { get; }
    
    /// <summary>
    /// Sender name
    /// </summary>
    string Sender { get; }
    
    /// <summary>
    /// Must you send reply back?
    /// </summary>
    bool IsReplyRequired { get; }

    /// <summary>
    /// Send reply message back to sender
    /// </summary>
    void Reply<TReply>(TReply message);
}