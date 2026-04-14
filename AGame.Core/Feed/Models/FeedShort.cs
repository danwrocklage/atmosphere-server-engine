namespace AGame.Core.Feed.Models;

public class FeedShort
{
    public string Title { get; set; }
        
    public string[] Tags { get; set; }
        
    public string Author { get; set; }
        
    public string[] Badges { get; set; }
        
    public string ImageUrl { get; set; }
        
    public DateTime PublishedAt { get; set; }
        
    public string ShortBody { get; set; }

    public string Slug { get; set; }
}