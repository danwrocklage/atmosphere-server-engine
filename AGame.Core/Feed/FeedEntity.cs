using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace AGame.Core.Feed;

/// <summary>
/// Game news/blog/event item
/// </summary>
[Table("feeds")]
internal class FeedEntity : IDbEntity
{
    /// <summary>
    /// ID
    /// </summary>
    public Guid Id { get; set; }
        
    /// <summary>
    /// Feed caption
    /// </summary>
    public string Title { get; set; }
        
    /// <summary>
    /// Feed related tags
    /// </summary>
    public string[] Tags { get; set; }
        
    /// <summary>
    /// Author display name
    /// </summary>
    public string Author { get; set; }
        
    /// <summary>
    /// Feed type badges (e.g. event, update, interview)
    /// </summary>
    public string[] Badges { get; set; }
        
    /// <summary>
    /// Cover image url
    /// </summary>
    public string ImageUrl { get; set; }
        
    /// <summary>
    /// Main part of feed as markdown
    /// </summary>
    public string Body { get; set; }
        
    /// <summary>
    /// Preview of main part (annotation)
    /// </summary>
    public string ShortBody { get; set; }
        
    /// <summary>
    /// Date of feed creation
    /// </summary>
    public DateTime CreatedAt { get; set; }
        
    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime UpdatedAt { get; set; }
        
    /// <summary>
    /// Date when feed has been visible for all
    /// </summary>
    public DateTime? PublishedAt { get; set; }
        
    /// <summary>
    /// If true, feed will be visible only for feed author staff user
    /// </summary>
    public bool IsDraft { get; set; }

    public bool IsPublished => PublishedAt.HasValue;
    
    /// <summary>
    /// Staff user who created this feed
    /// </summary>
    public Guid AuthorStaffId { get; set; } 
        
    /// <summary>
    /// Feed url section
    /// </summary>
    public string Slug { get; set; }
}