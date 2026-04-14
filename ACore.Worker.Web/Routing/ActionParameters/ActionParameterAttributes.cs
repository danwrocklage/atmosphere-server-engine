namespace ACore.Worker.Web.Routing;

/// <summary>
/// Get request body to action method
/// </summary>
/// <remarks>Use only with <see cref="Stream"/> type</remarks>
[AttributeUsage(AttributeTargets.Parameter)]
public class FromBodyAttribute : Attribute { }
    
/// <summary>
/// Bind request query parameter to method by argument name
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class FromQueryAttribute : Attribute { }
    
/// <summary>
/// Bind request header to method by name
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class FromHeaderAttribute : Attribute {
    public FromHeaderAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
    
/// <summary>
/// Bind request dynamic url segment to method
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class FromRouteAttribute : Attribute { }
    
/// <summary>
/// Get object from DI container
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class FromServiceAttribute : Attribute { }