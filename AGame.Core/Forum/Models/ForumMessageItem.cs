namespace AGame.Core.Forum.Models;

public class ForumMessageItem
{
    public Guid Id { get; set; }
        
    public string Message { get; set; }
        
    public DateTime CreatedAt { get; set; }
        
    public ForumAuthorInfo Author { get; set; }
}