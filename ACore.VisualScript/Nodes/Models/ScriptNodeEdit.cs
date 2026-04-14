namespace ACore.VisualScript.Models;

public class ScriptNodeEdit
{
    /// <summary>
    /// Node display name
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
    /// Node contexts
    /// </summary>
    public string[] Contexts { get; set; }
        
    /// <summary>
    /// Node group (when node list displaying)
    /// </summary>
    public string Group { get; set; }
        
    /// <summary>
    /// Node key words
    /// </summary>
    public string[] Tags { get; set; }
        
    /// <summary>
    /// Node color
    /// </summary>
    public string Color { get; set; }

    /// <summary>
    /// Input sockets
    /// </summary>
    public NodeEndpoint[] Input { get; set; }
        
    /// <summary>
    /// Output results
    /// </summary>
    public NodeEndpoint[] Output { get; set; }
}