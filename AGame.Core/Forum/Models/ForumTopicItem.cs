namespace AGame.Core.Forum.Models;

public class ForumTopicItem
{
    public Guid Id { get; set; }
        
    public string Name { get; set; }
        
    public bool IsOpen { get; set; }
        
    public int MessageCount { get; set; }
        
    public DateTime CreatedAt { get; set; }
        
    public ForumAuthorInfo TopicAuthor { get; set; }

    public DateTime LastMessageAt { get; set; }
        
    public ForumAuthorInfo LastMessageAuthor { get; set; }
}

public class ForumAuthorInfo
{
    public Guid AuthorId { get; set; }
        
    public string AuthorName { get; set; }
}