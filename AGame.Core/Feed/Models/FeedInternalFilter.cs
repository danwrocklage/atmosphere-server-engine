namespace AGame.Core.Feed.Models;

public class FeedInternalFilter
{
    public int? Page { get; set; }
        
    public int Size { get; set; }
        
    public string[] Tags { get; set; }
        
    public string[] Badges { get; set; }
    
    public Guid? AuthorId { get; set; }
    
    public bool? IsDraft { get; set; }
    
    public bool? IsPublished { get; set; }
}