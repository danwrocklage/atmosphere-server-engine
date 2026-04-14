using ACore.Abstractions;
using ACore.Abstractions.Rpc;
using ACore.Abstractions.Storage;

namespace AGame.Actors.Avatar;

public sealed class AvatarContext
{
    private readonly ICellCluster mCellCluster;
    private readonly IStorageHash<Guid> mActorsCells;

    public AvatarContext(IRpc rpc, IStorage storage, ICellCluster cellCluster)
    {
        Rpc = rpc;
        mCellCluster = cellCluster;
        mActorsCells = storage.HashOf<Guid>(StorageTopics.ACTOR_IDS);
    }

    internal IRpc Rpc { get; }

    public async Task<long> GetActorsCount(CancellationToken token = default)
    {
        var mechanics = mCellCluster.Cells
            .Where(x => x.Role == Cell.MECHANICS)
            .Select(x => x.AppId).ToArray();

        var total = 0l;
        foreach (var mechanic in mechanics)
        {
            total += await Rpc.Call<ActorCountEvent, int>(
                $"{RpcTopics.ACTOR_COUNT}.{mechanic}", 
                new ActorCountEvent(),
                token);
        }

        return total;
    }

    public Task<AvatarOf<T>> Create<T>(string name = null, bool isThin = false, CancellationToken token = default) where T : Actor =>
        Create<T>(null, null, name, isThin, token);

    internal async Task<AvatarOf<T>> Create<T>(Guid? actorId = null, Guid? parentId = null, string name = null, bool isThin = false, CancellationToken token = default)
        where T : Actor
    {
        var createdActor = await Rpc.Call<CreateActorRequest, CreateActorResponse>(RpcTopics.ACTOR_CREATE, new CreateActorRequest
        {
            ActorId = actorId,
            ParentId = parentId,
            Name = name,
            Type = typeof(T).AssemblyQualifiedName,
            IsThin = isThin
        }, token);

        return !createdActor.IsSuccess ? default : new AvatarOf<T>(createdActor.ActorId, createdActor.CellId, this);
    }

    public async Task<bool> Destroy(Guid actorId, CancellationToken token = default)
    {
        var cellId = await mActorsCells.Get(actorId.ToString());
        var topic = $"{RpcTopics.ACTOR_DESTROY}.{cellId}";
        return await Rpc.Call<DestroyRequest, bool>(topic, new DestroyRequest
        {
            ActorId = actorId
        }, token);
    }

    public async Task<AvatarOf<T>> Get<T>(Guid actorId)
    {
        var cellId = await mActorsCells.Get(actorId.ToString());
        if (cellId == default)
            return default;

        if (!mCellCluster.IsCellIdExists(cellId))
        {
            await mActorsCells.Delete(actorId.ToString());
            return default;
        }
        
        return new AvatarOf<T>(actorId, cellId, this);
    }
}