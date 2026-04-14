namespace ACore.Abstractions.Rpc;

/// <summary>
/// Remote procedure call service interface
/// </summary>
public interface IRpc
{
    /// <summary>
    /// Remote call with reply. Topic is taken from class name
    /// </summary>
    /// <param name="request">Sending message</param>
    /// <param name="token">Cancellation token</param>
    /// <typeparam name="TRequest">Sending message type</typeparam>
    /// <typeparam name="TReply">Receiving message type</typeparam>
    /// <returns>Response message from remote</returns>
    Task<TReply> Call<TRequest, TReply>(TRequest request, CancellationToken token = default);

    /// <summary>
    /// Remote call with reply
    /// </summary>
    /// <param name="topic">Channel name</param>
    /// <param name="request">Sending message</param>
    /// <param name="token">Cancellation token</param>
    /// <typeparam name="TRequest">Sending message type</typeparam>
    /// <typeparam name="TReply">Receiving message type</typeparam>
    /// <returns>Response message from remote</returns>
    Task<TReply> Call<TRequest, TReply>(string topic, TRequest request, CancellationToken token = default);

    /// <summary>
    /// Remote call without reply (fire and forget). Topic is taken from class name
    /// </summary>
    /// <param name="request">Sending message</param>
    /// <param name="token">Cancellation token</param>
    /// <typeparam name="TRequest">Sending message type</typeparam>
    Task Call<TRequest>(TRequest request, CancellationToken token = default);

    /// <summary>
    /// Remote call without reply (fire and forget)
    /// </summary>
    /// <param name="topic">Channel name</param>
    /// <param name="request">Sending message</param>
    /// <param name="token">Cancellation token</param>
    /// <typeparam name="TRequest">Sending message type</typeparam>
    Task Call<TRequest>(string topic, TRequest request, CancellationToken token = default);
}