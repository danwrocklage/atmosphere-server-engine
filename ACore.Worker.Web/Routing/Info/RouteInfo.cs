using System.Reflection;

namespace ACore.Worker.Web.Routing.Info;

public class RouteInfo
{
    /// <summary>
    /// Action method
    /// </summary>
    public MethodInfo Action { get; init; }
        
    /// <summary>
    /// Url template path
    /// </summary>
    public string Path { get; init; }
        
    /// <summary>
    /// Http method
    /// </summary>
    public string Method { get; init; }
    
    /// <summary>
    /// Controller type
    /// </summary>
    public Type Controller => Action.DeclaringType;
        
    public Parameter[] Parameters { get; init; }
    
    public class Parameter
    {
        private static readonly IDictionary<RouteParameterType, string> sRouteInfoParameterNames = Enum
            .GetValues<RouteParameterType>()
            .ToDictionary(x => x, x => Enum.GetName(x));
        
        public string Name { get; init; }
        
        public Type Type { get; init; }
        
        internal RouteParameterType ParameterTypeValue { private get; init; }

        public string ParameterType => sRouteInfoParameterNames[ParameterTypeValue];

        public bool IsRequired => ParameterTypeValue == RouteParameterType.Route;
    }

    internal enum RouteParameterType : byte
    {
        Route,
        Query,
        Header,
        Body
    }
}