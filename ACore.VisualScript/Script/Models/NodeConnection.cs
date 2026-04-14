namespace ACore.VisualScript.Models;

public class NodeConnection
{
    public bool IsFlow { get; set; }

    public bool IsOutput { get; set; }

    public string Name { get; set; }
        
    public string NodeId { get; set; }
        
    public string EndpointName { get; set; }

    public override string ToString() =>
        $"{(IsFlow ? "Flow" : string.Empty)}{(IsOutput ? "Out" : string.Empty)}Connection:{Name} to {NodeId}:{(!string.IsNullOrEmpty(EndpointName) ? $":{EndpointName}" : string.Empty)}";
}