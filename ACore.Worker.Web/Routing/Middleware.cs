using System.Net;

namespace ACore.Worker.Web.Routing;

/// <summary>
/// Http processing class base for all requests
/// </summary>
public abstract class Middleware
{
    /// <summary>
    /// Delegate of next step in processing pipeline
    /// </summary>
    protected internal Func<HttpListenerContext, Session, CancellationToken, Task> Next { get; internal set; }
        
    /// <summary>
    /// Run request processing
    /// </summary>
    public abstract Task Execute(HttpListenerContext context, Session session, CancellationToken token = default);
}