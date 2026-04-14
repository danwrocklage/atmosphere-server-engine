namespace AGame.Core.Forum.Models;

public class ForumTopicFilter
{
    public int? Page { get; set; }
        
    public int Size { get; set; }
        
    public string TopicName { get; set; }
        
    public bool IsValid => Page is not < 0 && Size > 0;
}