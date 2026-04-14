using System.Reflection;
using ACore.Abstractions.Logging;
using ACore.Worker.Web.Routing.Attributes;
using ACore.Worker.Web.Routing.Info;
using IContainer = AUtils.IoC.IContainer;

namespace ACore.Worker.Web.Routing;

/// <summary>
/// Endpoints descriptions storage
/// </summary>
public class RouteManager
{
    private static readonly Type[] sHttpMethodsAttributesTypes = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(x => x.BaseType == typeof(RouteAttribute))
        .ToArray();

    private bool mIsInitialized;
    private readonly List<Route> mRoutes = new();
    private readonly List<RouteInfo> mRouteInfos = new();

    internal IReadOnlyCollection<Route> Routes => mRoutes;
        
    /// <summary>
    /// Route descriptions list
    /// </summary>
    public IReadOnlyCollection<RouteInfo> RouteInfos => mRouteInfos;

    /// <summary>
    /// Get all available endpoints from controllers and store its
    /// </summary>
    internal void Initialize(List<Type> controllers, IContainer container)
    {
        if(mIsInitialized)
            return;

        container.Resolve<ILogger>()
            .Log("http.web", $"Initialize controllers {controllers.Count.ToString()}", LogLevel.Debug);
            
        foreach (var controller in controllers)
        {
            var routePath = controller.GetCustomAttribute<RoutePrefixAttribute>(true)?.Path ??
                            controller.Name.ToLowerInvariant().Replace("controller", "");
                
            var publicMethods = GetControllerActionMethods(controller);
                
            var leaf = Route.CreateRouteFromPathAndAttach(routePath, mRoutes);

            foreach (var (route, actionMethod) in publicMethods)
            {
                var httpMethod = Router.HttpMethodsString[route.Method];
                if (string.IsNullOrEmpty(route.Path))
                {
                    if(leaf.Handlers.ContainsKey(httpMethod))
                        throw new InvalidOperationException($"{controller.Name} ambiguous route {httpMethod}");
                        
                    leaf.Handlers.Add(httpMethod, new ActionHandler(actionMethod, controller, container));
                    continue;
                }
                    
                var actionLeaf = Route.CreateRouteFromPathAndAttach(route.Path, leaf.Children);
                if(actionLeaf.Handlers.ContainsKey(httpMethod))
                    throw new InvalidOperationException($"{controller.Name} ambiguous route {httpMethod} {route.Path}");
                    
                actionLeaf.Handlers.Add(httpMethod, new ActionHandler(actionMethod, controller, container));
            }
        }

        var excludedNamespaces = new[] { "ACore.Worker.Web" };
        foreach (var route in mRoutes)
            GetRouteInfo(string.Empty, route, mRouteInfos, excludedNamespaces);

        mIsInitialized = true;
    }

    private static (RouteAttribute, MethodInfo)[] GetControllerActionMethods(Type controller)
    {
        var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public);

        var actions = new List<(RouteAttribute, MethodInfo)>();
        foreach (var method in methods)
        {
            var routeAttributes = method.GetCustomAttributes(false)
                .Where(x => sHttpMethodsAttributesTypes.Contains(x.GetType()))
                .ToArray();
                
            if(routeAttributes.Length < 1)
                continue;
                
            if(routeAttributes.Length > 1)
                throw new InvalidOperationException($"{controller.Name} ambiguous action {method.Name}");

            actions.Add(((RouteAttribute)routeAttributes[0], method));
        }

        return actions.ToArray();
    }
        
    private void GetRouteInfo(string basePath, Route route, List<RouteInfo> result, string[] excludedNamespaces)
    {
        var path = string.Concat(basePath, "/", route.Path);
        foreach (var handler in route.Handlers)
        {
            var (action, parameters) = handler.Value.Description;

            var ns = action.DeclaringType?.Namespace ?? string.Empty;
            if(excludedNamespaces.Any(x => ns.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
                continue;
                
            result.Add(new RouteInfo
            {
                Action = action,
                Path = path,
                Method = handler.Key,
                Parameters = parameters
                    .Where(x => 
                        x.Value.Item2 != ActionParameterType.Service &&
                        x.Value.Item2 != ActionParameterType.CancellationToken &&
                        x.Value.Item2 != ActionParameterType.Stream)
                    .Select(x => new RouteInfo.Parameter
                    {
                        Name = x.Key,
                        Type = x.Value.Item1,
                        ParameterTypeValue = x.Value.Item2 switch
                        {
                            ActionParameterType.Body => RouteInfo.RouteParameterType.Body,
                            ActionParameterType.Header => RouteInfo.RouteParameterType.Header,
                            ActionParameterType.Query => RouteInfo.RouteParameterType.Header,
                            ActionParameterType.Route => RouteInfo.RouteParameterType.Route,
                            null => route.Path.Contains(x.Key, StringComparison.InvariantCultureIgnoreCase) ? 
                                RouteInfo.RouteParameterType.Route : 
                                RouteInfo.RouteParameterType.Query,
                            _ => throw new ArgumentOutOfRangeException()
                        }
                    })
                    .ToArray()
            });
        }

        foreach (var routeChild in route.Children)
            GetRouteInfo(path, routeChild, result, excludedNamespaces);
    }
}