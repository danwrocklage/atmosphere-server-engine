using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using AGame.Core.Account;
using AGame.Core.Forum.Entities;
using AGame.Core.Forum.Models;
using AGame.Core.Journal;
using AGame.Core.Staff;

namespace AGame.Core.Forum;

internal class ForumService : IForumService
{
    private readonly IDatabase mDatabase;
    private readonly ILogger<ForumService> mLogger;
    private readonly IJournalService mJournalService;

    public ForumService(IDatabase database, ILogger<ForumService> logger, IJournalService journalService)
    {
        mDatabase = database;
        mLogger = logger;
        mJournalService = journalService;
    }

    public async Task<List<ForumTopicItem>> GetTopics(ForumTopicFilter filter, Guid authorId)
    {
        if (filter is not { IsValid: true })
            return new List<ForumTopicItem>(0);

        var query = mDatabase.Repository<ForumTopicEntity>()
            .Select()
            .Where(x => x.IsOpen || x.AuthorId == authorId)
            .Join<AccountEntity, ForumTopicItem>(x => x.AuthorId, (x, a) => new ForumTopicItem
            {
                Id = x.Id,
                Name = x.Name,
                IsOpen = x.IsOpen,
                CreatedAt = x.CreatedAt,
                TopicAuthor = new ForumAuthorInfo
                {
                    AuthorId = x.AuthorId,
                    AuthorName = a.Name
                }
            });

        if (filter.Page is > 0)
        {
            query = query
                .Skip(filter.Page.Value * filter.Size)
                .Take(filter.Size);
        }

        var result = await query
            .OrderByDescending(x => x.LastMessageAt)
            .ToListAsync();

        return result;
    }

    public async Task<List<ForumMessageItem>> GetTopicMessages(Guid topicId, ForumMessageFilter filter)
    {
        if (filter is not { IsValid: true })
            return new List<ForumMessageItem>(0);
        if (!await IsTopicExists(topicId))
        {
            mLogger.Debug($"Topic {topicId} doesn't exist");
            return new List<ForumMessageItem>(0);
        }

        var query = mDatabase.Repository<ForumMessageEntity>().Select()
            .Where(x => x.TopicId == topicId && !x.IsDeleted)
            .Select(x => new ForumMessageItem
            {
                Id = x.Id,
                Message = x.Message,
                CreatedAt = x.CreatedAt,
                Author = new ForumAuthorInfo
                {
                    AuthorId = x.AuthorId
                }
            });

        if (filter.Page is > 0)
        {
            query = query
                .Skip(filter.Page.Value * filter.Size)
                .Take(filter.Size);
        }

        var result = await query.ToListAsync();

        return result;
    }

    public async Task<bool> CloseTopic(Guid topicId, Guid authorId)
    {
        if (!await IsTopicExists(topicId, authorId))
        {
            mLogger.Warn($"Can't set {nameof(ForumTopicEntity.IsOpen)} to {false} for {topicId} (author: {authorId})");
            return false;
        }
            
        mLogger.Info($"Set {nameof(ForumTopicEntity.IsOpen)} to {false} for {topicId} (author: {authorId})");
        await mDatabase
            .Repository<ForumTopicEntity>()
            .Update(topicId).Set(x => x.IsOpen, false).Apply();

        await mJournalService.Write($"Set {nameof(ForumTopicEntity.IsOpen)} to {false} for {topicId} (author: {authorId})", "Forum",
            JournalLink.Create<ForumTopicEntity>(topicId));

        return true;
    }

    public async Task<bool> PostTopicMessage(Guid topicId, string message, Guid authorId)
    {
        if (!await IsTopicExists(topicId))
        {
            mLogger.Warn($"Can't post new message. Topic {topicId} doesn't exist");
            return false;
        }
            
        mLogger.Info($"New message for {topicId} from {authorId}");
        await mDatabase
            .Repository<ForumMessageEntity>()
            .Insert(new ForumMessageEntity
            {
                Id = Guid.NewGuid(),
                AuthorId = authorId,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                TopicId = topicId
            });

        return true;
    }

    public async Task<bool> PostTopicMessage(string topicName, bool isImportant, string message, Guid authorId)
    {
        mLogger.Info($"New topic {(isImportant ? "important" : string.Empty)} '{topicName}' from {authorId}");
        var topicId = Guid.NewGuid();
        await mDatabase
            .Repository<ForumTopicEntity>()
            .Insert(new ForumTopicEntity
            {
                Id = topicId,
                Name = topicName,
                AuthorId = authorId,
                CreatedAt = DateTime.UtcNow,
                IsImportant = false,
                IsOpen = true,
                IsPinned = false
            });

        await mDatabase
            .Repository<ForumMessageEntity>()
            .Insert(new ForumMessageEntity
            {
                Id = Guid.NewGuid(),
                AuthorId = authorId,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                TopicId = topicId
            });

        if (isImportant)
            await mDatabase.Repository<ForumModerationRequestEntity>()
                .Insert(new ForumModerationRequestEntity
                {
                    Id = Guid.NewGuid(),
                    IsImportant = true,
                    TopicId = topicId,
                    ProcessedStaffId = null
                });

        return true;
    }

    public async Task SetProcessedModerationRequest(Guid moderationRequestId, Guid staffId)
    {
        mLogger.Info($"Moderation request was processed {moderationRequestId}");
        await mDatabase
            .Repository<ForumModerationRequestEntity>()
            .Update(moderationRequestId).Set(x => x.ProcessedStaffId, staffId).Apply();
    }

    public async Task SetTopicImportant(Guid topicId, bool isImportant, Guid staffId)
    {
        if (!await IsTopicExists(topicId))
        {
            mLogger.Warn($"Can't set {nameof(ForumTopicEntity.IsImportant)} to {isImportant} for {topicId} (staff: {staffId})");
            return;
        }
            
        mLogger.Info($"Set {nameof(ForumTopicEntity.IsImportant)} to {isImportant} for {topicId} (staff: {staffId})");
        await mDatabase
            .Repository<ForumTopicEntity>()
            .Update(topicId).Set(x => x.IsImportant, isImportant).Apply();

        await mJournalService.Write($"Set {nameof(ForumTopicEntity.IsImportant)} to {isImportant} for {topicId}", "Forum",
            JournalLink.Create<ForumTopicEntity>(topicId), 
            JournalLink.Create<StaffEntity>(staffId));
    }

    public async Task SetTopicPinned(Guid topicId, bool isPinned, Guid staffId)
    {
        if (!await IsTopicExists(topicId))
        {
            mLogger.Warn($"Can't set {nameof(ForumTopicEntity.IsPinned)} to {isPinned} for {topicId} (staff: {staffId})");
            return;
        }
            
        mLogger.Info($"Set {nameof(ForumTopicEntity.IsPinned)} to {isPinned} for {topicId} (staff: {staffId})");
        await mDatabase
            .Repository<ForumTopicEntity>()
            .Update(topicId).Set(x => x.IsPinned, isPinned).Apply();

        await mJournalService.Write($"Set {nameof(ForumTopicEntity.IsPinned)} to {isPinned} for {topicId}", "Forum",
            JournalLink.Create<ForumTopicEntity>(topicId), 
            JournalLink.Create<StaffEntity>(staffId));
    }

    public async Task SetTopicClosed(Guid topicId, bool isClosed, Guid staffId)
    {
        if (!await IsTopicExists(topicId))
        {
            mLogger.Warn($"Can't set {nameof(ForumTopicEntity.IsOpen)} to {(!isClosed).ToString()} for {topicId} (staff: {staffId})");
            return;
        }
            
        mLogger.Info($"Set {nameof(ForumTopicEntity.IsOpen)} to {(!isClosed).ToString()} for {topicId} (staff: {staffId})");
        await mDatabase
            .Repository<ForumTopicEntity>()
            .Update(topicId).Set(x => x.IsOpen, !isClosed).Apply();

        await mJournalService.Write($"Set {nameof(ForumTopicEntity.IsOpen)} to {(!isClosed).ToString()} for {topicId}", "Forum",
            JournalLink.Create<ForumTopicEntity>(topicId), 
            JournalLink.Create<StaffEntity>(staffId));
    }

    public async Task DeleteTopicMessage(Guid topicId, Guid messageId, Guid staffId)
    {
        if (!await IsMessageExists(topicId, messageId))
        {
            mLogger.Warn($"Can't delete topic message {messageId} (topic: {topicId}, staff: {staffId})");
            return;
        }
            
        mLogger.Info($"Delete topic message {messageId} (topic: {topicId}, staff: {staffId})");
        await mDatabase
            .Repository<ForumMessageEntity>()
            .Update(topicId).Set(x => x.IsDeleted, true).Apply();

        await mJournalService.Write($"Delete topic message {messageId}", "Forum",
            JournalLink.Create<ForumMessageEntity>(messageId), 
            JournalLink.Create<ForumTopicEntity>(topicId), 
            JournalLink.Create<StaffEntity>(staffId));
    }

    private Task<bool> IsTopicExists(Guid topicId, Guid? authorId = null) =>
        mDatabase.Repository<ForumTopicEntity>().Select()
            .AnyAsync(x => x.Id == topicId && (authorId == null || x.AuthorId == authorId));
        
    private async Task<bool> IsMessageExists(Guid topicId, Guid messageId, Guid? authorId = null) =>
        await mDatabase.Repository<ForumMessageEntity>().Select()
            .AnyAsync(x => x.Id == messageId && x.TopicId == topicId && (authorId == null || x.AuthorId == authorId)) &&
        await IsTopicExists(topicId);
}