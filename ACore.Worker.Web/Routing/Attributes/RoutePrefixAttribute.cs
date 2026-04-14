namespace ACore.Worker.Web.Routing.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class RoutePrefixAttribute : Attribute
{
    public RoutePrefixAttribute(string path)
    {
        Path = path;
    }

    public string Path { get; }
}