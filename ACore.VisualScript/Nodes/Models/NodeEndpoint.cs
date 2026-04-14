namespace ACore.VisualScript.Models;

public class NodeEndpoint
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
        
    /// <summary>
    /// Node text description (docs)
    /// </summary>
    public string Description { get; set; }
        
    /// <summary>
    /// Node color
    /// </summary>
    public string Color { get; set; }

    public override string ToString() => $"{(IsFlow ? "Flow" : string.Empty)}Endpoint:{Name}:{Type}";
}