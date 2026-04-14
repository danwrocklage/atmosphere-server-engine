namespace ACore.VisualScript.Models;

internal class NodeUnitDescription
{
    /// <summary>
    /// Unique node type
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Input sockets
    /// </summary>
    public NodeEndpoint[] Input { get; set; }
        
    /// <summary>
    /// Output results
    /// </summary>
    public NodeEndpoint[] Output { get; set; }

    /// <summary>
    /// Is a node a flow node
    /// </summary>
    public bool IsFlow() => 
        Input.Any(x => x.IsFlow) || 
        Output.Any(x => x.IsFlow);
}