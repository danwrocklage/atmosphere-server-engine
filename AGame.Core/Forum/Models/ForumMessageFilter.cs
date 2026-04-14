namespace AGame.Core.Forum.Models;

public class ForumMessageFilter
{
    public int? Page { get; set; }
        
    public int Size { get; set; }

    public bool IsValid => Page is not < 0 && Size is > 0 and <= 50;
}