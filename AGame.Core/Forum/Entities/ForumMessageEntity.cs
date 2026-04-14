using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace AGame.Core.Forum.Entities;

[Table("forum.messages")]
public class ForumMessageEntity : IDbEntity
{
    public Guid Id { get; set; }
        
    public Guid TopicId { get; set; }
        
    public Guid AuthorId { get; set; }
        
    public string Message { get; set; }
        
    public DateTime CreatedAt { get; set; }
        
    public bool IsDeleted { get; set; }
}