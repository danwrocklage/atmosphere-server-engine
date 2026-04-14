namespace AGame.Core.Feed.Models;

public class FeedFilter
{
    public int? Page { get; set; }
        
    public int Size { get; set; }
        
    public string[] Tags { get; set; }
        
    public string[] Badges { get; set; }
}