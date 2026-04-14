namespace ACore.VisualScript.Models;

public class ScriptNodeFilter
{
    /// <summary>
    /// Query for name, group and description
    /// </summary>
    public string Search { get; set; }
        
    /// <summary>
    /// Workspace
    /// </summary>
    public string Context { get; set; }
        
    /// <summary>
    /// Unique node type
    /// </summary>
    public string Type { get; set; }
        
    /// <summary>
    /// Search for next node, otherwise previous
    /// </summary>
    public bool IsForward { get; set; }
        
    /// <summary>
    /// Node can have prev flow node
    /// </summary>
    public bool? HasFlowIn { get; set; }
        
    /// <summary>
    /// Node can have next flow node
    /// </summary>
    public bool? HasFlowOut { get; set; }
}