using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;
using ACore.VisualScript.Models;

namespace ACore.VisualScript;

[Table("visualscript")]
public class ScriptEntity : IDbEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; }
        
    public string? Group { get; set; }
        
    public Guid AuthorId { get; set; }
        
    public DateTime CreatedAt { get; set; }
        
    public DateTime UpdatedAt { get; set; }
        
    public bool IsCompiled { get; set; }
        
    public IReadOnlyCollection<ScriptItem> Items { get; set; }
}