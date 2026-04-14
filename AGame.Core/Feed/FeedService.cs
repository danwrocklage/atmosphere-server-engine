using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using AGame.Core.Feed.Models;

namespace AGame.Core.Feed;

internal class FeedService : IFeedService
{
    private readonly IRepository<FeedEntity> mFeeds;
    private readonly ILogger<FeedService> mLogger;

    public FeedService(IDatabase database, ILogger<FeedService> logger)
    {
        mLogger = logger;
        mFeeds = database.Repository<FeedEntity>();
    }

    public Task<FeedDetails> GetFeedBySlug(string slug)
    {
        if (string.IsNullOrEmpty(slug)) 
            throw new ArgumentNullException(nameof(slug));

        return mFeeds.Select()
            .Where(x => x.IsPublished && string.Equals(x.Slug, slug, StringComparison.InvariantCultureIgnoreCase))
            .Select(x => new FeedDetails
            {
                Author = x.Author,
                Badges = x.Badges,
                Body = x.Body,
                Slug = x.Slug,
                Tags = x.Tags,
                Title = x.Title,
                ImageUrl = x.ImageUrl,
                PublishedAt = x.PublishedAt ?? default
            })
            .FirstOrDefaultAsync();
    }

    public Task<List<FeedShort>> GetFeeds(FeedFilter filter)
    {
        if (filter == null) 
            throw new ArgumentNullException(nameof(filter));
            
        return mFeeds.Select()
            .Where(x => x.IsPublished && 
                        (filter.Badges.Length == 0 || x.Badges.Intersect(filter.Badges).Any()) && 
                        (filter.Tags.Length == 0 || x.Tags.Intersect(filter.Tags).Any()))
            .Select(x => new FeedShort
            {
                Author = x.Author,
                Badges = x.Badges,
                ShortBody = x.ShortBody,
                Slug = x.Slug,
                Tags = x.Tags,
                Title = x.Title,
                ImageUrl = x.ImageUrl,
                PublishedAt = x.PublishedAt ?? default
            })
            .Skip((filter.Page ?? default) * filter.Size)
            .Take(filter.Size)
            .ToListAsync();
    }

    public Task<List<FeedFull>> GetFeedsInternal(FeedInternalFilter filter, Guid staffId)
    {
        if (filter == null) 
            throw new ArgumentNullException(nameof(filter));
            
        return mFeeds.Select()
            .Where(x => 
                (filter.IsPublished == null || x.IsPublished == filter.IsPublished.Value) && 
                (filter.IsDraft == null ? !x.IsDraft || x.AuthorStaffId == staffId : (filter.IsDraft.Value ? x.IsDraft && x.AuthorStaffId == staffId : !x.IsDraft)) && 
                (filter.AuthorId == null || x.AuthorStaffId == filter.AuthorId.Value) && 
                (filter.Badges.Length == 0 || x.Badges.Intersect(filter.Badges).Any()) && 
                (filter.Tags.Length == 0 || x.Tags.Intersect(filter.Tags).Any()))
            .Select(x => ToFullModel(x))
            .Skip((filter.Page ?? default) * filter.Size)
            .Take(filter.Size)
            .ToListAsync();
    }

    public Task<FeedFull> GetFeed(Guid id)
    {
        if(id == default)
            throw new ArgumentException(string.Empty, nameof(id));

        return mFeeds.Select()
            .Where(x => x.Id == id)
            .Select(x => ToFullModel(x))
            .FirstOrDefaultAsync();
    }

    public Task AddFeed(EditFeed model)
    {
        if(model == null)
            throw new ArgumentNullException(nameof(model));
            
        mLogger.Info($"Add new feed '{model.Title}'");

        return mFeeds.Insert(new FeedEntity
        {
            Id = Guid.NewGuid(),
            Author = model.Author,
            Badges = model.Badges,
            Body = model.Body,
            Slug = model.Slug,
            Title = model.Title,
            Tags = model.Tags,
            CreatedAt = DateTime.UtcNow,
            ImageUrl = model.ImageUrl,
            IsDraft = true,
            PublishedAt = null,
            ShortBody = model.ShortBody,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public Task UpdateFeed(Guid id, EditFeed model)
    {
        if(id == default)
            throw new ArgumentNullException(nameof(id));

        if(model == null)
            throw new ArgumentNullException(nameof(model));

        mLogger.Info($"Update feed [{id.ToString()}] '{model.Title}'");

        return mFeeds.Update(id)
            .Set(x => x.Title, model.Title)
            .Set(x => x.Author, model.Author)
            .Set(x => x.Body, model.Body)
            .Set(x => x.Slug, model.Slug)
            .Set(x => x.ImageUrl, model.ImageUrl)
            .Set(x => x.ShortBody, model.ShortBody)
            .Set(x => x.Badges, model.Badges)
            .Set(x => x.Tags, model.Tags)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Apply();
    }

    public Task DeleteFeed(Guid id)
    {
        if(id == default)
            throw new ArgumentNullException(nameof(id));

        mLogger.Info($"Delete feed [{id.ToString()}]");
        return mFeeds.Delete(x => x.Id == id);
    }

    public Task SetPublish(Guid id, bool isPublish)
    {
        if(id == default)
            throw new ArgumentNullException(nameof(id));
            
        mLogger.Info($"Set feed [{id.ToString()}] publish to {isPublish.ToString()}");
        return mFeeds.Update(id)
            .Set(x => x.PublishedAt, DateTime.UtcNow)
            .Apply();
    }

    public Task SetDraft(Guid id, bool isDraft)
    {
        if(id == default)
            throw new ArgumentNullException(nameof(id));
            
        mLogger.Info($"Set feed [{id.ToString()}] draft to {isDraft.ToString()}");
        return mFeeds.Update(id) 
            .Set(x => x.IsDraft, isDraft)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Apply();
    }
    
    private static FeedFull ToFullModel(FeedEntity x) =>
        new()
        {
            Author = x.Author,
            Badges = x.Badges,
            Body = x.Body,
            Slug = x.Slug,
            Tags = x.Tags,
            Title = x.Title,
            ImageUrl = x.ImageUrl,
            PublishedAt = x.PublishedAt,
            Id = x.Id,
            CreatedAt = x.CreatedAt,
            IsDraft = x.IsDraft,
            IsPublished = x.IsPublished,
            ShortBody = x.ShortBody,
            UpdatedAt = x.UpdatedAt
        };
}