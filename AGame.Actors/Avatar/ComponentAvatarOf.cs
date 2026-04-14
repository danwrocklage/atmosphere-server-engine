using System.Diagnostics;
using System.Linq.Expressions;

namespace AGame.Actors.Avatar;

[DebuggerDisplay("{ToString()} Actor:{OwnerId} Cell:{mBase.CellId}")]
public readonly struct ComponentAvatarOf<T> where T : ActorComponent
{
    private readonly string mName;
    private readonly Guid mOwnerId;
    private readonly AvatarBaseOf<T> mBase;
    private readonly bool mIsInitialized;

    internal ComponentAvatarOf(Guid cellId, AvatarContext avatarContext, Guid ownerId, string name)
    {
        if(cellId == default)
            throw new ArgumentException(nameof(cellId));
        
        if(ownerId == default)
            throw new ArgumentException(nameof(ownerId));

        if (avatarContext == null) 
            throw new ArgumentNullException(nameof(avatarContext));

        mOwnerId = ownerId;
        mName = name;
        mBase = new AvatarBaseOf<T>(cellId, avatarContext);
        mIsInitialized = true;
    }

    public bool IsEmpty => !mIsInitialized;

    public Guid OwnerId => mIsInitialized ? mOwnerId : throw new ActorException("Actor component is empty");

    public string Name => mIsInitialized ? mName : throw new ActorException("Actor component is empty");

    public Task<bool> Remove()
    {
        if(!mIsInitialized)
            throw new ActorException("Actor component is empty");
        
        return mBase.AvatarContext.Rpc.Call<ComponentRequest, bool>(new ComponentRequest
        {
            ActorId = OwnerId,
            Component = new RpcComponent {Name = Name, Type = typeof(T).AssemblyQualifiedName},
            Type = ComponentRequestType.Remove
        });
    }

    #region RPC

    public Task Rpc<TItem>(Expression<Func<T, TItem>> expression, TItem value, CancellationToken token = default)
    {
        if(!mIsInitialized)
            throw new ActorException("Actor component is empty");

        return mBase.Rpc(expression, OwnerId, value, (typeof(T), Name), token);
    }
    
    public Task Rpc(Expression<Action<T>> expression, CancellationToken token = default)
    {
        if(!mIsInitialized)
            throw new ActorException("Actor component is empty");
    
        return mBase.Rpc(expression, OwnerId, (typeof(T), Name), token);
    }

    public Task Rpc(Expression<Func<T, Task>> expression, CancellationToken token = default)
    {
        if(!mIsInitialized)
            throw new ActorException("Actor component is empty");
        
        return mBase.Rpc(expression, OwnerId, (typeof(T), Name), token);
    }

    public Task<TItem> Rpc<TItem>(Expression<Func<T, TItem>> expression, CancellationToken token = default)
    {
        if(!mIsInitialized)
            throw new ActorException("Actor component is empty");
        
        return mBase.Rpc(expression, OwnerId, (typeof(T), Name), token);
    }

    public Task<TItem> Rpc<TItem>(Expression<Func<T, Task<TItem>>> expression, CancellationToken token = default)
    {
        if(!mIsInitialized)
            throw new ActorException("Actor component is empty");
        
        return mBase.Rpc(expression, OwnerId, (typeof(T), Name), token);
    }

    #endregion

    public override string ToString() => 
        $"Component<{typeof(T).Name}>({Name})";
}