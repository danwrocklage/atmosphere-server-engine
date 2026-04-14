using ACore.Abstractions.Database;

namespace AGame.Core.Journal;

internal class JournalService : IJournalService
{
    private readonly IRepository<JournalEntity> mJournal;

    public JournalService(IDatabase database)
    {
        mJournal = database.Repository<JournalEntity>();
    }
        
    public Task Write(string message, string category)
    {
        if(string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));
            
        return mJournal.Insert(new JournalEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Category = category,
            Message = message,
            Links = null
        });
    }

    public Task Write<T>(Guid id, string message, string category = null)
    {
        if(string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));
            
        return mJournal.Insert(new JournalEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Category = string.IsNullOrEmpty(category) ? typeof(T).Name : category,
            Message = message,
            Links = new []{ new JournalLink {Id = id.ToString(), Type = typeof(T).FullName} }
        });
    }
        
    public Task Write(string message, string category, params IDbEntity[] links)
    {
        if(links == null)
            throw new ArgumentNullException(nameof(links));
            
        if(string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));

        var journalLinks = new JournalLink[links.Length];
        for (int i = 0; i < links.Length; i++)
        {
            journalLinks[i] = new JournalLink
            {
                Id = links[i].Id.ToString(),
                Type = links[i].GetType().FullName
            };
        }
            
        return mJournal.Insert(new JournalEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Category = category,
            Message = message,
            Links = journalLinks
        });
    }
        
    public Task Write(string message, string category, params JournalLink[] links)
    {
        if(links == null)
            throw new ArgumentNullException(nameof(links));
            
        if(string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));

        if(links.Any(x => string.IsNullOrEmpty(x?.Id) || string.IsNullOrEmpty(x.Type)))
            throw new ArgumentNullException(nameof(links));

        return mJournal.Insert(new JournalEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Category = category,
            Message = message,
            Links = links
        });
    }

    public Task<List<JournalEntity>> GetByLinkOf<T>(Guid id)
    {
        var typeStr = typeof(T).FullName;
        var idStr = id.ToString();
        return mJournal.Select()
            .Where(x => x.Links.Any(j => j.Type == typeStr && j.Id == idStr))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }
        
    public Task<List<JournalEntity>> GetByCategory(string category)
    {
        if (string.IsNullOrEmpty(category)) 
            throw new ArgumentNullException(nameof(category));

        return mJournal.Select()
            .Where(x => x.Category == category)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }
}