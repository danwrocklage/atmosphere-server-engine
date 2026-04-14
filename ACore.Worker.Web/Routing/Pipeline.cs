using System.Net;
using System.Text.Json;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Telemetry;
using AUtils.IoC;

namespace ACore.Worker.Web.Routing;

/// <summary>
/// Pipeline for HTTP requests processing
/// </summary>
[Log(Category = "http.pipeline")]
internal class Pipeline
{
    private const string HTTP_SERVER_NAME = "ACore-HTTP-Web";

    private static readonly JsonSerializerOptions sExceptionSerializerOptions = new()
    {
        Converters = {new ExceptionConverter()}
    };

    private readonly string[] mMetricsLabels;
    private readonly IContainer mContainer;
    private readonly RouteManager mRouteManager;
    private readonly Router mRouter;
    private readonly ILogger<Pipeline> mLogger;
    private readonly ICellMetrics mMetrics;

    private Func<HttpListenerContext, Session, CancellationToken, Task> mPipelineExecutor;

    internal Pipeline(Router router,
        ILogger<Pipeline> logger,
        ICellMetrics metrics,
        RouteManager routeManager,
        IContainer container,
        ICellEnvironment environment)
    {
        mRouter = router;
        mLogger = logger;
        mMetrics = metrics;
        mRouteManager = routeManager;
        mContainer = container;
        mMetricsLabels = new[] {string.Empty, environment.Role};
    }

    /// <summary>
    /// Initialize route manager and create pipeline execution chain
    /// </summary>
    internal void Initialize(Middleware[] middlewares, List<Type> controllers)
    {
        mRouteManager.Initialize(controllers, mContainer);

        if (middlewares.Length > 0)
        {
            var length = middlewares.Length;
            for (var i = 0; i < length; i++)
            {
                middlewares[i].Next = i == length - 1
                    ? mRouter.Execute
                    : middlewares[i + 1].Execute;
            }

            mPipelineExecutor = middlewares[0].Execute;
        }
        else
            mPipelineExecutor = mRouter.Execute;
    }

    public async Task Execute(HttpListenerContext listenerContext, CancellationToken token = default)
    {
        var session = new Session();

        listenerContext.Response.ContentType = "application/json";

        mMetricsLabels[0] = $"{listenerContext.Request.HttpMethod} {listenerContext.Request.Url?.LocalPath}";

        try
        {
            mMetrics.Get("cell_http_request_count").Inc(mMetricsLabels);
            mRouter.StoreMatchedRouteToSession(listenerContext, session);
            await mPipelineExecutor(listenerContext, session, token);
        }
        catch (Exception e)
        {
            mMetrics.Get("cell_http_request_fail_count").Inc(mMetricsLabels);
            mLogger.Error(listenerContext.Request.Url?.ToString(), e);
            listenerContext.Response.StatusCode = 500;
            await JsonSerializer.SerializeAsync(listenerContext.Response.OutputStream, e, sExceptionSerializerOptions, token);
        }

        listenerContext.Response.Headers[HttpResponseHeader.Server] = HTTP_SERVER_NAME;
        listenerContext.Response.Close();

        session.Release();
    }
}