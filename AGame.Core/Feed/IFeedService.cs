using AGame.Core.Feed.Models;

namespace AGame.Core.Feed;

/// <summary>
/// Service for manage publications
/// </summary>
public interface IFeedService
{
    #region Public api

    /// <summary>
    /// Get published feed by url
    /// </summary>
    Task<FeedDetails> GetFeedBySlug(string slug);

    /// <summary>
    /// Get published feeds list. Visible for all 
    /// </summary>
    Task<List<FeedShort>> GetFeeds(FeedFilter filter);

    #endregion

    /// <summary>
    /// Get all feeds list. Use for administration
    /// </summary>
    Task<List<FeedFull>> GetFeedsInternal(FeedInternalFilter filter, Guid staffId);

    /// <summary>
    /// Get feed by id
    /// </summary>
    Task<FeedFull> GetFeed(Guid id);
        
    /// <summary>
    /// Create new feed
    /// </summary>
    Task AddFeed(EditFeed model);

    /// <summary>
    /// Update already existed feed
    /// </summary>
    Task UpdateFeed(Guid id, EditFeed model);
        
    /// <summary>
    /// Delete feed
    /// </summary>
    Task DeleteFeed(Guid id);

    /// <summary>
    /// Set visible for all
    /// </summary>
    Task SetPublish(Guid id, bool isPublish);

    /// <summary>
    /// Set visible for other staff users
    /// </summary>
    Task SetDraft(Guid id, bool isDraft);
}