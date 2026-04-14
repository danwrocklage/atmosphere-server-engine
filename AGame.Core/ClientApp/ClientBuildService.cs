using ACore.Abstractions;
using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Rpc;
using AGame.Core.Journal;

namespace AGame.Core.ClientApp;

internal class ClientBuildService : IClientBuildService
{
    private readonly ILogger<ClientBuildService> mLogger;
    private readonly IJournalService mJournalService;
    private readonly IRpc mRpc;
    private readonly IRepository<ClientBuildEntity> mRepository;

    public ClientBuildService(IDatabase database, ILogger<ClientBuildService> logger, 
        IJournalService journalService, IRpc rpc)
    {
        mRepository = database.Repository<ClientBuildEntity>();
        mLogger = logger;
        mJournalService = journalService;
        mRpc = rpc;
    }

    public async Task CreateNewVersion(NewClientBuild model)
    {
        if (model == null) 
            throw new ArgumentNullException(nameof(model));

        var versionId = Guid.NewGuid();
        await mRepository.Insert(new ClientBuildEntity
        {
            Id = versionId,
            BuildType = model.BuildType,
            Version = model.Version,
            CreatedAt = DateTime.UtcNow
        });
        
        await mJournalService.Write<ClientBuildEntity>(versionId, $"Client build was created {model.BuildType:G}");
        mLogger.Info($"Client build '{model.Version}' was created ({model.BuildType:G})");
        await mRpc.Call(new GlobalNotificationEvent
            {Message = $"[{model.BuildType:G}] New {model.Type:G} client build - {model.Version}"});
    }

    public Task<List<ClientBuildItem>> GetVersions() =>
        mRepository.Select()
            .Select(x => new ClientBuildItem
            {
                Id = x.Id,
                BuildType = x.BuildType,
                Type = x.Type,
                Version = x.Version,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

    public async Task<bool> IsVersionSupported(string version)
    {
        if (string.IsNullOrEmpty(version))
#if DEBUG
            return true;
#else
            throw new ArgumentNullException(nameof(version));
#endif
        
        return await mRepository.Select()
            .AnyAsync(x => x.Version == version);
    }

    public Task<string> GetCurrentVersion(ClientBuildType type) =>
        mRepository.Select().Where(x => x.BuildType == type)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Version)
            .FirstOrDefaultAsync();

    public async Task ChangeType(Guid id, ClientBuildType type)
    {
        var isModified = await mRepository.Update(id)
            .Set(x => x.BuildType, type)
            .Apply() > 0;
        
        if(!isModified)
            return;
        
        await mJournalService.Write<ClientBuildEntity>(id, $"Change type to {type:G} for client build");
        mLogger.Info($"Client build {id} type was changed to {type:G}");
    }

    public async Task DeleteVersion(Guid id)
    {
        var isDeleted = await mRepository.Delete(x => x.Id == id) > 0;

        if(isDeleted)
        {
            mLogger.Info($"Client build {id} was deleted");
            await mJournalService.Write<ClientBuildEntity>(id, "Client build was deleted");
        }
        else
            mLogger.Warn($"Client build {id} was not deleted");
    }
}