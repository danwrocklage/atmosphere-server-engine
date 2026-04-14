namespace ACore.Worker.Web.Routing;

/// <summary>
/// Request endpoint
/// </summary>
internal class Route
{
    /// <summary>
    /// Url segment
    /// </summary>
    public string Path { get; set; }
        
    /// <summary>
    /// Is current url segment dynamic route value?
    /// </summary>
    public bool IsParam { get; set; }
        
    /// <summary>
    /// Children url segments
    /// </summary>
    public List<Route> Children { get; } = new();
        
    /// <summary>
    /// Handlers for current url segment stored by (HttpMethod, HandlerDelegate)
    /// </summary>
    public Dictionary<string, ActionHandler> Handlers { get; } = new();
        
    /// <summary>
    /// Generate route from controller url and add it to <paramref name="routes"/>
    /// </summary>
    internal static Route CreateRouteFromPathAndAttach(string routePath, List<Route> routes)
    {
        if(string.IsNullOrEmpty(routePath))
            throw new ArgumentException(nameof(routePath));
            
        var segments = routePath.Split('/');
            
        var root = routes.FirstOrDefault(x => x.Path == segments[0]);
        if (root == null)
        {
            routes.Add(new Route 
            {
                Path = segments[0], 
                IsParam = IsRouteParam(segments[0])
            });
            root = routes.Last();
        }

        var currentRoute = root;
        for (var i = 1; i < segments.Length; i++)
        {
            var child = currentRoute.Children.FirstOrDefault(x => x.Path == segments[i]);
            if (child != null)
            {
                currentRoute = child;
                continue;
            }
                
            currentRoute.Children.Add(new Route
            {
                Path = segments[i],
                IsParam = IsRouteParam(segments[i])
            });
            currentRoute = currentRoute.Children.Last();
        }

        return currentRoute;
    }

    /// <summary>
    /// Check: if url segment is dynamic route value
    /// </summary>
    private static bool IsRouteParam(string routePath) => 
        !string.IsNullOrWhiteSpace(routePath) && routePath[0] == '{' && routePath[^1] == '}';
}