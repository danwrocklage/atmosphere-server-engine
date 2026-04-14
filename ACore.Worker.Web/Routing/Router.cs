using System.Net;
using ACore.Abstractions;

namespace ACore.Worker.Web.Routing;

/// <summary>
/// Model for requested route with parameters
/// </summary>
internal record MatchedRoute(Route Route, IReadOnlyDictionary<string, string> UrlParameters); 
    
/// <summary>
/// Class for matching and executing handlers for requests
/// </summary>
internal class Router
{
    internal const string MATCHED_ROUTE_KEY = "matched_route";
        
    internal static readonly Dictionary<HttpMethod, string> HttpMethodsString =
        Enum.GetValues<HttpMethod>().ToDictionary(x => x, x => Enum.GetName(x)?.ToUpperInvariant());
        
    private readonly RouteManager mRouteManager;
    private readonly string[] mPrefixSegments;
    private readonly int mPrefixesLength;

    public Router(IConfiguration configuration, RouteManager routeManager)
    {
        mRouteManager = routeManager;
        var config = configuration.Get(() => WebWorkerConfig.Default);
        mPrefixSegments = config.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        mPrefixesLength = mPrefixSegments.Length;
    }

    /// <summary>
    /// Found matched route for request and store it to <paramref name="session"/>
    /// </summary>
    public void StoreMatchedRouteToSession(HttpListenerContext context, Session session)
    {
        var segments = context.Request.Url?.Segments
            .Select(x => Uri.UnescapeDataString(x.TrimEnd('/')))
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();
        session.Add(MATCHED_ROUTE_KEY, GetRouteWithParams(segments));
    }

    /// <summary>
    /// Process request and run handler
    /// </summary>
    public async Task Execute(HttpListenerContext context, Session session, CancellationToken token = default)
    {
        var (route, urlParams) = session.Get<MatchedRoute>(MATCHED_ROUTE_KEY);

        if (route == null || route.Handlers.Count == 0)
        {
            context.Response.StatusCode = 404;
            return;
        }

        if (context.Request.HttpMethod == HttpMethodsString[HttpMethod.Options])
        {
            context.Response.Headers[HttpResponseHeader.Allow] = string.Join(", ", route.Handlers.Keys);
            context.Response.StatusCode = 200;
            return;
        }
            
        if(!route.Handlers.ContainsKey(context.Request.HttpMethod))
        {
            context.Response.StatusCode = 405;
            return;
        }

        await route.Handlers[context.Request.HttpMethod].Handle(urlParams, context, session, token);
    }

    private MatchedRoute GetRouteWithParams(string[] urlSegments)
    {
        var routeParams = new Dictionary<string, string>(0);
        Route current = null;
        var length = urlSegments.Length;
        for (var i = 0; i < length; i++)
        {
            if (i < mPrefixesLength)
            {
                if(urlSegments[i] == mPrefixSegments[i])
                    continue;
                break;
            }
                
            current = GetMatchedRoute(urlSegments[i], current?.Children ?? mRouteManager.Routes);
            if (current == null)
                break;
                
            if(current.IsParam)
                routeParams.Add(GetUrlParamName(current.Path), urlSegments[i]);
        }

        return new MatchedRoute(current, current == null ? null : routeParams);
    }

    private static Route GetMatchedRoute(string path, IReadOnlyCollection<Route> routes) => 
        routes.FirstOrDefault(x => x.Path == path && !x.IsParam) ?? 
        routes.FirstOrDefault(x => x.IsParam);

    private static string GetUrlParamName(string path) => 
        path.Substring(1, path.Length - 2);
}