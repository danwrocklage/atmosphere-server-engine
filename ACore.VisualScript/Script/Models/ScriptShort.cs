namespace ACore.VisualScript.Models;

public class ScriptShort
{
    public Guid Id { get; set; }

    public string Name { get; set; }
        
    public string? Group { get; set; }
        
    public int ItemsCount { get; set; }
        
    public bool IsCompiled { get; set; }
}