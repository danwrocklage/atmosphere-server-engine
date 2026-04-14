namespace ACore.VisualScript.Models;

public record NodePosition(float X, float Y);
    
public class ScriptItem
{
    public string Id { get; set; }
        
    public NodePosition Position { get; set; }
        
    public string Type { get; set; }
        
    public Dictionary<string, string> Values { get; set; }
        
    public NodeConnection[] Connections { get; set; }

    public override string ToString() => $"SchemaItem:{Type}:{Id}";
}