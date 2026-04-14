namespace ACore.Worker.Web.Routing.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class RouteAttribute : Attribute
{
    protected RouteAttribute(HttpMethod method, string path)
    {
        Method = method;
        Path = path;
    }

    public string Path { get; }
    public HttpMethod Method { get; }
}
    
[AttributeUsage(AttributeTargets.Method)]
public class GetAttribute : RouteAttribute
{
    public GetAttribute(string path = null) 
        : base(HttpMethod.Get, path) { }
}
    
[AttributeUsage(AttributeTargets.Method)]
public class PatchAttribute : RouteAttribute
{
    public PatchAttribute(string path = null) 
        : base(HttpMethod.Patch, path) { }
}
    
[AttributeUsage(AttributeTargets.Method)]
public class PostAttribute : RouteAttribute
{
    public PostAttribute(string path = null) 
        : base(HttpMethod.Post, path) { }
}
    
[AttributeUsage(AttributeTargets.Method)]
public class PutAttribute : RouteAttribute
{
    public PutAttribute(string path = null) 
        : base(HttpMethod.Put, path) { }
}
    
[AttributeUsage(AttributeTargets.Method)]
public class DeleteAttribute : RouteAttribute
{
    public DeleteAttribute(string path = null) 
        : base(HttpMethod.Delete, path) { }
}