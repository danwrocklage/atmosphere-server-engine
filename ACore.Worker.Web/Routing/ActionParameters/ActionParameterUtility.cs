using System.Reflection;

namespace ACore.Worker.Web.Routing;

internal static class ActionParameterUtility
{
    private static readonly Dictionary<string, ActionParameterType> sActionParameterTypes = new()
    {
        {typeof(FromBodyAttribute).FullName ?? throw new Exception(), ActionParameterType.Body},
        {typeof(FromHeaderAttribute).FullName ?? throw new Exception(), ActionParameterType.Header},
        {typeof(FromQueryAttribute).FullName ?? throw new Exception(), ActionParameterType.Query},
        {typeof(FromRouteAttribute).FullName ?? throw new Exception(), ActionParameterType.Route},
        {typeof(FromServiceAttribute).FullName ?? throw new Exception(), ActionParameterType.Service}
    };

    internal static IReadOnlyDictionary<string, (Type, ActionParameterType?)> GetParametersMeta(ParameterInfo[] parameters)
    {
        var args = new Dictionary<string, (Type, ActionParameterType?)>();

        foreach (var parameter in parameters)
        {
            var (name, actionType) = GetActionParameterMeta(parameter);
            args.Add(name, (parameter.ParameterType, actionType));
        }

        return args;
    }
        
    private static (string, ActionParameterType?) GetActionParameterMeta(ParameterInfo parameter)
    {
        if (parameter == null) 
            throw new ArgumentNullException(nameof(parameter));
            
        var name = parameter.Name?.ToLowerInvariant();

        if (parameter.ParameterType == typeof(CancellationToken))
            return (name, ActionParameterType.CancellationToken);
        
        var attributes = parameter.GetCustomAttributes()
            .Select(x => x.GetType().FullName)
            .ToArray();

        ActionParameterType? result = null;
        foreach (var attribute in attributes)
        {
            if (!sActionParameterTypes.TryGetValue(attribute, out var type))
                continue;
                
            if(result.HasValue)
                throw new InvalidOperationException();

            result = type;
        }
            
        if (result == ActionParameterType.Header)
            name = parameter.GetCustomAttribute<FromHeaderAttribute>()?.Name;

        if (parameter.ParameterType == typeof(Stream) && !result.HasValue)
            result = ActionParameterType.Stream;
            
        return (name, result);
    }
}