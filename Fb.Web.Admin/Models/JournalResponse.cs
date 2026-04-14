namespace Fb.Web.Admin.Models;

public class JournalResponse
{
    public Guid Id { get; set; }
        
    public string Category { get; set; }
        
    public string Message { get; set; }
        
    public DateTime CreatedAt { get; set; }
        
    public JournalLink[] Links { get; set; }
}

public class JournalLink
{
    public string Type { get; set; }
        
    public string Id { get; set; }
}