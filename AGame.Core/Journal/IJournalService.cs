using ACore.Abstractions.Database;

namespace AGame.Core.Journal;

public interface IJournalService
{
    Task Write(string message, string category);
    Task Write<T>(Guid id, string message, string category = null);
    Task Write(string message, string category, params IDbEntity[] links);
    Task Write(string message, string category, params JournalLink[] links);
    Task<List<JournalEntity>> GetByLinkOf<T>(Guid id);
    Task<List<JournalEntity>> GetByCategory(string category);
}