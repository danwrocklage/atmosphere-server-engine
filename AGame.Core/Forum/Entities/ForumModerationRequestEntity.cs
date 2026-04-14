using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace AGame.Core.Forum.Entities;

[Table("forum.moderation_requests")]
public class ForumModerationRequestEntity : IDbEntity
{
    public Guid Id { get; set; }
        
    public Guid? ProcessedStaffId { get; set; }
        
    public Guid TopicId { get; set; }
        
    public bool IsImportant { get; set; }
}