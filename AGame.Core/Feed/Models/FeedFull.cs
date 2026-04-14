namespace AGame.Core.Feed.Models;

public class FeedFull
{
    public Guid Id { get; set; }
        
    public string Title { get; set; }
        
    public string[] Tags { get; set; }
        
    public string Author { get; set; }
        
    public string[] Badges { get; set; }
        
    public string ImageUrl { get; set; }
        
    public string Body { get; set; }
        
    public string ShortBody { get; set; }
        
    public DateTime CreatedAt { get; set; }
        
    public DateTime UpdatedAt { get; set; }
        
    public DateTime? PublishedAt { get; set; }
        
    public bool IsDraft { get; set; }
        
    public bool IsPublished { get; set; }
        
    public string Slug { get; set; }
}