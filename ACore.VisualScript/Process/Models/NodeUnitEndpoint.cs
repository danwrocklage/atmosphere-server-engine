namespace ACore.VisualScript.Models;

internal class NodeUnitEndpoint
{
    /// <summary>
    /// Is the socket flow
    /// </summary>
    public bool IsFlow => string.IsNullOrEmpty(Type);
        
    /// <summary>
    /// Socket display name
    /// </summary>
    public string Name { get; set; }
        
    /// <summary>
    /// Unique node type
    /// </summary>
    public string Type { get; set; }

    public override string ToString() => $"{Name}:{(IsFlow ? "Flow" : Type)}";
}