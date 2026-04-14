using System.Diagnostics;
using System.Linq.Expressions;

namespace AGame.Actors.Avatar;

[DebuggerDisplay("{ToString()} Cell:{mBase.CellId}")]
public readonly struct AvatarOf<T>
{
    private readonly Guid mId;
    private readonly AvatarBaseOf<T> mBase;
    private readonly bool mIsInitialized;

    internal AvatarOf(Guid id, Guid cellId, AvatarContext avatarContext)
    {
        mId = id;
        mIsInitialized = true;
        mBase = new AvatarBaseOf<T>(cellId, avatarContext);
    }

    public bool IsEmpty => !mIsInitialized;

    public Guid Id => mIsInitialized ? mId : throw new ActorException("Actor is empty");
    
    public Task<AvatarOf<TChildActor>> Create<TChildActor>(string name = null, bool isThin = false) where TChildActor : Actor
    {
        if(IsEmpty)
            throw new ActorException("Actor is empty");
        
        return mBase.AvatarContext.Create<TChildActor>(null, Id, name, isThin);
    }

    public Task<bool> Destroy()
    {
        if(!mIsInitialized)
            throw new ActorException("Actor is empty");

        return mBase.AvatarContext.Destroy(Id);
    }
    
    /// <summary>
    /// Send new actor event of <typeparamref name="T"/>
    /// </summary>
    public void Emit<TEvent>(TEvent actorEvent, Guid[] targetIds = null) where TEvent : class
    {
        if(actorEvent == null)
            return;

        mBase.AvatarContext.Rpc.Call(RpcTopics.ACTOR_EVENT, new Eventing.ActorEvent
        {
            Payload = actorEvent,
            Type = Eventing.ActorEventType.Event,
            SenderActorId = Id,
            TargetActorIds = targetIds
        });
    }

    public async Task<ComponentAvatarOf<TComponent>> Add<TComponent>(string name = null, CancellationToken token = default) where TComponent : ActorComponent
    {
        if(!mIsInitialized)
            throw new ActorException("Actor is empty");

        var response = await mBase.AvatarContext.Rpc.Call<ComponentRequest, bool>(new ComponentRequest
        {
            ActorId = Id,
            Component = new RpcComponent {Name = name, Type = typeof(TComponent).AssemblyQualifiedName},
            Type = ComponentRequestType.Create
        }, token);

        return response ? 
            new ComponentAvatarOf<TComponent>(mBase.CellId, mBase.AvatarContext, Id, name) : 
            default;
    }
    
    public async Task<ComponentAvatarOf<TComponent>> Get<TComponent>(string name = null, CancellationToken token = default) where TComponent : ActorComponent
    {
        if(!mIsInitialized)
            throw new ActorException("Actor is empty");

        var response = await mBase.AvatarContext.Rpc.Call<ComponentRequest, bool>(new ComponentRequest
        {
            ActorId = Id,
            Component = new RpcComponent {Name = name, Type = typeof(TComponent).AssemblyQualifiedName},
            Type = ComponentRequestType.Get
        }, token);

        return response ? 
            new ComponentAvatarOf<TComponent>(mBase.CellId, mBase.AvatarContext, Id, name) : 
            default;
    }

    public Task<bool> Remove<TComponent>(string name = null, CancellationToken token = default) where TComponent : ActorComponent
    {
        if(!mIsInitialized)
            throw new ActorException("Actor is empty");

        return mBase.AvatarContext.Rpc.Call<ComponentRequest, bool>(new ComponentRequest
        {
            ActorId = Id,
            Component = new RpcComponent {Name = name, Type = typeof(TComponent).AssemblyQualifiedName},
            Type = ComponentRequestType.Remove
        }, token);
    }

    #region RPC
    
    public Task Rpc<TItem>(Expression<Func<T, TItem>> expression, TItem value, CancellationToken token = default)
    {
        if(!mIsInitialized)
            throw new ActorException("Actor component is empty");

        return mBase.Rpc(expression, Id, value, token: token);
    }

    public Task Rpc(Expression<Action<T>> expression, CancellationToken token = default)
    {
        if(!mIsInitialized)
            throw new ActorException("Actor is empty");

        return mBase.Rpc(expression, Id, token: token);
    }

    public Task Rpc(Expression<Func<T, Task>> expression, CancellationToken token = default)
    {
        if(!mIsInitialized)
            throw new ActorException("Actor is empty");
        
        return mBase.Rpc(expression, Id, token: token);
    }

    public Task<TItem> Rpc<TItem>(Expression<Func<T, TItem>> expression, CancellationToken token = default)
    {
        if(!mIsInitialized)
            throw new ActorException("Actor is empty");
        
        return mBase.Rpc(expression, Id, token: token);
    }

    public Task<TItem> Rpc<TItem>(Expression<Func<T, Task<TItem>>> expression, CancellationToken token = default)
    {
        if(!mIsInitialized)
            throw new ActorException("Actor is empty");
        
        return mBase.Rpc(expression, Id, token: token);
    }

    #endregion

    public AvatarOf<TCast> To<TCast>() => new(mId, mBase.CellId, mBase.AvatarContext);

    public override string ToString() => !mIsInitialized ? 
        $"Empty Actor<{typeof(T).Name}>" : $"Actor<{typeof(T).Name}>({Id.ToString()})";
}