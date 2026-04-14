using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace AGame.Core.Journal;

[Table("journals")]
public class JournalEntity : IDbEntity
{
    public Guid Id { get; set; }
        
    public string Category { get; set; }
        
    public string Message { get; set; }
        
    public DateTime CreatedAt { get; set; }
        
    public JournalLink[] Links { get; set; }
}

public class JournalLink
{
    public string Type { get; set; }
        
    public string Id { get; set; }

    public static JournalLink Create<T>(Guid id) => new() { Id = id.ToString(), Type = typeof(T).FullName };
}