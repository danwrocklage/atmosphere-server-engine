using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Storage;

namespace AGame.Core.Identity;

/// <inheritdoc />
[Log(Category = "Identity")]
internal class IdentityService : IIdentityService
{
    private const int DEFAULT_MAX_FAILS = 5;
    
    private readonly ILogger<IdentityService> mLogger;
    private readonly IDatabase mDatabase;
    private readonly IStorageHash<uint> mFailCounterHash;

    public IdentityService(IDatabase database, ILogger<IdentityService> logger, IStorage storage)
    {
        mDatabase = database;
        mLogger = logger;
        mFailCounterHash = storage.HashOf<uint>($"{nameof(Identity)}:fails-count");
    }

    /// <inheritdoc />
    public async Task<Guid> Create(string key, string secret, IdentityType type, string[] grandTypes)
    {
        if (string.IsNullOrEmpty(key)) 
            throw new ArgumentNullException(nameof(key));
        
        if (string.IsNullOrEmpty(secret)) 
            throw new ArgumentNullException(nameof(secret));
        
        if(grandTypes == null || grandTypes.Length == 0)
            throw new ArgumentNullException(nameof(grandTypes));

        var identityId = Guid.NewGuid();
        await mDatabase.Repository<Identity>()
            .Insert(new Identity
            {
                Id = identityId,
                Key = key,
                Secret = BCrypt.Net.BCrypt.EnhancedHashPassword(secret),
                Type = IdentityType.LoginPassword,
                GrandTypes = grandTypes.ToList(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                FailsAvailable = DEFAULT_MAX_FAILS
            });

        mLogger.Info($"New identity {identityId} was created");
        return identityId;
    }

    /// <inheritdoc />
    public Task<bool> Exists(string key, IdentityType type)
    {
        if (string.IsNullOrEmpty(key)) 
            throw new ArgumentNullException(nameof(key));
        
        return mDatabase.Select<Identity>().AnyAsync(x => x.Key == key && x.Type == type);
    }

    /// <inheritdoc />
    public Task<Identity> Get(Guid id) => 
        mDatabase.Select<Identity>().FirstOrDefaultAsync(x => x.Id == id);

    /// <inheritdoc />
    public async Task Link(Guid identityId, Guid linkedEntityId, string linkType)
    {
        if (linkedEntityId == Guid.Empty) 
            throw new ArgumentNullException(nameof(linkedEntityId));
        
        if (linkType == null) 
            throw new ArgumentNullException(nameof(linkType));

        await mDatabase.Repository<Identity>()
            .Update(identityId)
            .Set(x => x.Link, new IdentityLink {Id = linkedEntityId, Type = linkType})
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Apply();
        
        mLogger.Info($"Identity {identityId} was linked to ({linkedEntityId} of '{linkType}')");
    }

    public Task RemoveLink(Guid identityId, Guid linkedEntityId)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<(Identity Identity, bool ShouldBeBlocked)> Authorize(string @public, string @private, bool countFails)
    {
        if (string.IsNullOrEmpty(@public))
            throw new ArgumentNullException(nameof(@public));
        
        if (string.IsNullOrEmpty(@private)) 
            throw new ArgumentNullException(nameof(@private));
        
        var id = await mDatabase.Repository<Identity>().Select()
            .FirstOrDefaultAsync(x => x.Key == @public);
        if (id == null)
            return default;
        
        var key = id.Id.ToString();
        if (countFails && await mFailCounterHash.Get(key) > id.FailsAvailable) 
            return (new Identity {Id = id.Id, Link = id.Link}, true);

        if (BCrypt.Net.BCrypt.EnhancedVerify(@private, id.Secret))
        {
            await ResetFailsCounter(id.Id);
            return (id, false);
        }

        if (!countFails) 
            return default;
        
        await mFailCounterHash.Increment(key);
        return await mFailCounterHash.Get(key) > id.FailsAvailable ? 
            (new Identity {Id = id.Id, Link = id.Link}, true) : default;
    }

    /// <inheritdoc />
    public Task ResetFailsCounter(Guid identityId) => 
        mFailCounterHash.Store(identityId.ToString(), 0);
}