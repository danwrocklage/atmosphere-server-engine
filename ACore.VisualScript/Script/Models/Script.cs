namespace ACore.VisualScript.Models;

public class Script
{
    public string Name { get; set; }
        
    public string? Group { get; set; }
        
    public IReadOnlyCollection<ScriptItem> Items { get; set; }

    public override string ToString() => $"Script:{Name}:{Group}";
}