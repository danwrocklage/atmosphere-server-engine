using AGame.Core.Forum.Models;

namespace AGame.Core.Forum;

public interface IForumService
{
    #region Public api

    Task<List<ForumTopicItem>> GetTopics(ForumTopicFilter filter, Guid authorId);

    Task<List<ForumMessageItem>> GetTopicMessages(Guid topicId, ForumMessageFilter filter);

    Task<bool> CloseTopic(Guid topicId, Guid authorId);

    Task<bool> PostTopicMessage(Guid topicId, string message, Guid authorId);
        
    Task<bool> PostTopicMessage(string topicName, bool isImportant, string message, Guid authorId);

    #endregion

    Task SetProcessedModerationRequest(Guid moderationRequestId, Guid staffId);

    Task SetTopicImportant(Guid topicId, bool isImportant, Guid staffId);
    Task SetTopicPinned(Guid topicId, bool isPinned, Guid staffId);
    Task SetTopicClosed(Guid topicId, bool isClosed, Guid staffId);

    Task DeleteTopicMessage(Guid topicId, Guid messageId, Guid staffId);
}