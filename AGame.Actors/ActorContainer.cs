using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ACore.Abstractions;
using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Storage;
using AGame.Actors.Persistence;
using AUtils.IoC;

namespace AGame.Actors;

/// <summary>
/// Memory storage for all active actors
/// </summary>
[DebuggerDisplay("(Actors = {mActors.Count})")]
internal class ActorContainer : IAsyncInitializable
{
    private readonly int mMaxActorsCount;

    private readonly IContainer mContainer;
    private readonly IStorageHash<Guid> mActorIds;
    private readonly IRepository<ActorEntity> mActorsRepository;
    
    private readonly ConcurrentDictionary<Guid, Actor> mActors;
    private readonly ConcurrentQueue<Guid> mPendingRemoveActors;

    public ActorContainer(IConfiguration configuration, IStorage storage, IDatabase database, IContainer container)
    {
        mActorsRepository = database.Repository<ActorEntity>();
        mActorIds = storage.HashOf<Guid>(StorageTopics.ACTOR_IDS);
        mContainer = container;
        var config = configuration.Get(() => ActorsConfiguration.Default);
        MechanicsId = config.MechanicsId;
        mMaxActorsCount = config.MaxActorsCount;

        mActors = new ConcurrentDictionary<Guid, Actor>();
        mPendingRemoveActors = new ConcurrentQueue<Guid>();

        Actor.CreateContextIfNotExist(mContainer);
    }

    /// <summary>
    /// Load all stored in db actors by mechanicsId
    /// </summary>
    public async Task InitializeAsync()
    {
        var logger = mContainer.Resolve<ILogger<ActorContainer>>();
        
        logger.Debug($"Start loading actors by mechanics id = {MechanicsId}");
        await mActorsRepository.Update(x => x.AppId == null && x.MechanicsId == MechanicsId)
            .Set(x => x.AppId, Cell.AppId)
            .Apply();
        var query = mActorsRepository.Select().Where(x => x.AppId == null && x.MechanicsId == MechanicsId);
        var actorEntitiesCount = await query.CountAsync();

        if (actorEntitiesCount > mMaxActorsCount)
            query = query.Take(mMaxActorsCount);

        var actorEntities = await query.ToListAsync();

        logger.Debug($"Load {actorEntities.Count} actors");
        foreach (var actorEntity in actorEntities)
        {
            var actorType = Type.GetType(actorEntity.Type);
            if (actorType == null)
                continue;

            var actor = (Actor) mContainer.Resolve(actorType);
            actor.Id = actorEntity.Id;
            actor.Name = actorEntity.Name;
            actor.TickingMode = actorEntity.TickingMode;
            actor.IsEventReceiver = actorEntity.IsEventReceiver;
            PropertySerializer.Deserialize(actor, actorEntity.Properties);

            foreach (var componentEntity in actorEntity.Components)
            {
                var componentType = Type.GetType(componentEntity.Type);
                if (componentType == null)
                    continue;

                var component = (ActorComponent) mContainer.Resolve(componentType);
                component.IsTicking = componentEntity.IsTicking;
                PropertySerializer.Deserialize(component, componentEntity.Properties);
                actor.AddComponentInternal(component, componentEntity.Name);
            }

            mActors.TryAdd(actor.Id, actor);
            actor.OnMoved();
        }

        foreach (var actorEntity in actorEntities)
        {
            if (actorEntity.ChildrenIds.Length == 0 && actorEntity.ParentId == null)
                continue;

            var actor = GetActor(actorEntity.Id);
            if (actorEntity.ParentId.HasValue)
            {
                var parent = GetActor(actorEntity.ParentId.Value);
                parent?.AddChild(actor);
            }

            foreach (var childrenId in actorEntity.ChildrenIds)
            {
                var child = GetActor(childrenId);
                if (child == null)
                    continue;

                actor.AddChild(child);
            }
        }

        if (actorEntitiesCount < mMaxActorsCount)
            await mActorsRepository.Delete(x => x.MechanicsId == MechanicsId);
        else
        {
            var loadedActorsIds = actorEntities.Select(x => x.Id).ToArray();
            await mActorsRepository.Delete(x => loadedActorsIds.Contains(x.Id));
        }

        await mActorIds.Store(actorEntities.ToDictionary(x => x.Id.ToString(), _ => Cell.AppId));
        logger.Debug($"{actorEntities.Count} actors were loaded");
    }

    /// <summary>
    /// Get active actor by id
    /// </summary>
    public Actor GetActor(Guid id) => mActors.TryGetValue(id, out var actor) ? actor : null;

    /// <summary>
    /// All active actors
    /// </summary>
    public ICollection<Actor> Actors => mActors.Values;
    
    /// <summary>
    /// Mechanics id
    /// </summary>
    public string MechanicsId { get; }

    /// <summary>
    /// Create new actor
    /// </summary>
    public async Task<Actor> CreateActor(Guid? actorId, Type actorType, string name, bool isThin,
        Guid? parentActorId)
    {
        var actor = (Actor) mContainer.Resolve(actorType);
        await actor.InternalCreate(actorId, name, isThin, parentActorId.HasValue ? GetActor(parentActorId.Value) : null);
        
        if(!actor.IsThin)
            await mActorIds.Store(actor.Id.ToString(), Cell.AppId);

        return !mActors.TryAdd(actor.Id, actor) ? null : actor;
    }

    /// <summary>
    /// Remove actor
    /// </summary>
    internal async Task DestroyActor(Actor actor)
    {
        if (actor == null) 
            throw new ArgumentNullException(nameof(actor));
        
        await actor.InternalDestroy();
        mPendingRemoveActors.Enqueue(actor.Id);
        if(!actor.IsThin)
            await mActorIds.Delete(actor.Id.ToString());
    }

    public void CommitRemove()
    {
        while (!mPendingRemoveActors.IsEmpty)
        {
            if(!mPendingRemoveActors.TryDequeue(out var actorId))
                continue;

            mActors.TryRemove(actorId, out _);
        }
        mPendingRemoveActors.Clear();
    }

    public async Task StoreActors()
    {
        foreach (var actor in Actors)
        {
            if(actor.IsThin)
                continue;
            
            actor.OnBeforeMove();
        }
        
        var entities = Actors
            .Select(x => x.Serialize())
            .Where(x => x != null)
            .ToArray();
        if(entities.Length == 0)
            return;
        
        await mActorsRepository.Update(entities, true);
        var storage = mContainer.Resolve<IStorage>();
        if(storage != null)
            await storage.Transaction(async s =>
            {
                var actorsIds = s.HashOf<Guid>(StorageTopics.ACTOR_IDS);
                foreach (var actorEntity in entities)
                {
                    await actorsIds.Delete(actorEntity.Id.ToString());
                }
            });
    }
    
    /// <summary>
    /// Actor configuration
    /// </summary>
    [Configuration("actors")]
    [SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Local")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    private class ActorsConfiguration
    {
        /// <summary>
        /// Actors group id for current cell
        /// </summary>
        public string MechanicsId { get; set; }
        
        /// <summary>
        /// Max actor's count for cell
        /// </summary>
        public int MaxActorsCount { get; set; }

        public static ActorsConfiguration Default => new()
        {
            MechanicsId = "2c441772-02dd-49ea-a648-c389d24152e2",
            MaxActorsCount = 5000
        };
    }
}