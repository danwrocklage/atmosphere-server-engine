using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace AGame.Core.Forum.Entities;

[Table("forum.topics")]
public class ForumTopicEntity : IDbEntity
{
    public Guid Id { get; set; }
        
    public string Name { get; set; }
        
    public DateTime CreatedAt { get; set; }
        
    public Guid AuthorId { get; set; }
        
    public bool IsOpen { get; set; }
        
    public bool IsPinned { get; set; }
        
    public bool IsImportant { get; set; }
}