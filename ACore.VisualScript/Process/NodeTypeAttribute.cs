namespace ACore.VisualScript;

[AttributeUsage(AttributeTargets.Class)]
public class NodeTypeAttribute : Attribute
{
    public NodeTypeAttribute(string type)
    {
        Type = type;
    }

    public string Type { get; }
}