using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using ACore.Worker.Web.Routing;

namespace ACore.Worker.Web;

/// <summary>
/// Request temp storage
/// </summary>
public sealed class Session
{
    private readonly ConcurrentDictionary<string, object> mObjectPool;

    internal Session()
    {
        SessionId = Guid.NewGuid();
        mObjectPool = new ConcurrentDictionary<string, object>();
    }

    /// <summary>
    /// Request unique id
    /// </summary>
    public Guid SessionId { get; }

    /// <summary>
    /// Add item to storage by key
    /// </summary>
    public void Add<T>(string key, T value) => mObjectPool.TryAdd(key, value);

    /// <summary>
    /// Get item from storage by key
    /// </summary>
    public T Get<T>(string key) => mObjectPool.TryGetValue(key, out var value) ? (T) value : default;

    /// <summary>
    /// Clear storage
    /// </summary>
    internal void Release()
    {
        mObjectPool.Clear();
    }
}

public static class SessionExtensions
{
    private const string ENTITY_ID_KEY = "entity_id";

    /// <summary>
    /// Get action which will be executed by current request
    /// </summary>
    public static MethodInfo GetRouteAction(this Session session, HttpListenerContext context)
    {
        var matchedRoute = session.Get<MatchedRoute>(Router.MATCHED_ROUTE_KEY);
        return matchedRoute?.Route.Handlers.TryGetValue(context.Request.HttpMethod, out var handler) == true
            ? handler.Description?.Action
            : null;
    }

    /// <summary>
    /// Store user temp id
    /// </summary>
    public static void SetEntityId(this Session session, string tempId) => 
        session.Add(ENTITY_ID_KEY, tempId);

    /// <summary>
    /// Get user temp id
    /// </summary>
    public static string GetEntityId(this Session session) => 
        session.Get<string>(ENTITY_ID_KEY);
}