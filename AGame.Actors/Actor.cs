using System.Collections.Concurrent;
using System.Diagnostics;
using AGame.Actors.Eventing;
using AGame.Actors.Persistence;
using AGame.Actors.Replication;
using AUtils.IoC;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace AGame.Actors;

/// <summary>
/// Base class for all objects in a game world
/// </summary>
[DebuggerDisplay("{Name} (Components = {mComponents.Count})")]
public abstract class Actor
{
    private const string ACTORS_SCHEME = "actors";

    #region Global actor's context

    private static ActorContext sActorContext;

    internal static void CreateContextIfNotExist(IContainer container)
    {
        sActorContext ??= new ActorContext(container);
    }

    #endregion
    
    private readonly HashSet<Actor> mChildren = new();
    private readonly List<ActorComponent> mComponents = new();
    private ConcurrentQueue<(string, object)> mMailbox;
    private Dictionary<string, Delegate> mWatchers;
    private bool mIsEventReceiver;

    /// <summary>
    /// Actor's name
    /// </summary>
    internal string Name { get; set; }

    /// <summary>
    /// If true:<br/>
    /// - Actor is non persistent<br/>
    /// - Actor is non movable<br/>
    /// - Actor isn't tracked in storage<br/>
    /// - Actor doesn't emit create/destroy events (but <see cref="OnCreate"/> and <see cref="OnDestroy"/> are still called)
    /// </summary>
    internal bool IsThin { get; private set; }

    /// <summary>
    /// Actor unique id
    /// </summary>
    public Guid Id { get; internal set; }
    
    /// <summary>
    /// Where actor is living
    /// </summary>
    public Guid WorldId { get; internal set; }

    /// <summary>
    /// Is actor prepared for ticking?
    /// </summary>
    public bool IsInitialized => Id != Guid.Empty;

    /// <summary>
    /// What will be ticking (actor, components or both)
    /// </summary>
    protected internal TickingMode TickingMode { get; set; } = TickingMode.NoTicking;

    /// <summary>
    /// Can actor receive and process actor events
    /// </summary>
    protected internal bool IsEventReceiver
    {
        get => mIsEventReceiver;
        set
        {
            if (value)
            {
                mMailbox ??= new ConcurrentQueue<(string, object)>();
                mWatchers ??= new Dictionary<string, Delegate>();
            }
            
            mIsEventReceiver = value;
        }
    }

    /// <summary>
    /// Is actor removed from ticking and prepared for deletion
    /// </summary>
    internal bool IsDestroyed { get; private set; }

    public override string ToString()
    {
        if (!IsInitialized)
            return base.ToString();

        var root = Parent?.ToString() ?? $"{ACTORS_SCHEME}{Uri.SchemeDelimiter}{sActorContext.Actors.MechanicsId}";
        return $"{root}{Path.PathSeparator}{Name ?? Id.ToString()}";
    }

    /// <summary>
    /// Update value for property key to set available everywhere
    /// </summary>
    public void Replicate(string property, object value)
    {
        if (string.IsNullOrEmpty(property))
            throw new ArgumentException("Value cannot be null or empty.", nameof(property));
        
        sActorContext.ReplicationStorage.Set(Id, property, value);
        sActorContext.Rpc.Call(new ActorProperty {ActorId = Id, Value = value, Property = property});
    }

    /// <summary>
    /// Destroy current actor
    /// </summary>
    public void Destroy() => _ = sActorContext.Actors.DestroyActor(this);

    /// <summary>
    /// Create new child actor
    /// </summary>
    /// <param name="name">Name of new actor</param>
    /// <param name="isThin">Is new actor required to be saved</param>
    protected async Task<T> Create<T>(string name = null, bool isThin = false) where T : Actor => 
        (T) await Create(typeof(T), name, isThin);

    /// <summary>
    /// Create new child actor
    /// </summary>
    /// <param name="actorType">Type of new actor</param>
    /// <param name="name">Name of new actor</param>
    /// <param name="isThin">Is new actor required to be saved</param>
    protected async Task<Actor> Create(Type actorType, string name = null, bool isThin = false) => 
        await sActorContext.Actors.CreateActor(Guid.NewGuid(), actorType, name, isThin, Id);

    internal Task InternalCreate(Guid? actorId, string name, bool isThin, Actor creator = null)
    {
        Id = actorId ?? Guid.NewGuid();
        Name = name;
        IsThin = isThin;
        creator?.AddChild(this);

        if (!IsThin)
            sActorContext.Rpc.Call(RpcTopics.CREATE_EVENT, new ActorEvent
            {
                Type = ActorEventType.Create,
                SenderActorId = Id
            });
        return OnCreate();
    }

    #region Parent and children

    /// <summary>
    /// Actor, who created this actor
    /// </summary>
    protected internal Actor Parent { get; internal set; }

    /// <summary>
    /// Collection of all actors, which were created by this actor
    /// </summary>
    protected internal  IReadOnlyCollection<Actor> Children => mChildren;

    /// <summary>
    /// Add child actor and attach it to this
    /// </summary>
    /// <param name="child">created by this actor</param>
    /// <exception cref="ArgumentNullException"><paramref name="child"/> is null</exception>
    internal void AddChild(Actor child)
    {
        if (child == null)
            throw new ArgumentNullException(nameof(child));
        
        if(child.Parent != null)
            return;

        child.Parent = this;
        
        if(!mChildren.Contains(child))
            mChildren.Add(child);
    }

    /// <summary>
    /// Remove child actor from this actor and detach it
    /// </summary>
    /// <param name="child">created by this actor</param>
    /// <exception cref="ArgumentNullException"><paramref name="child"/> is null</exception>
    internal void RemoveChild(Actor child)
    {
        if (child == null)
            throw new ArgumentNullException(nameof(child));
        
        if(child.Parent != this)
            return;

        child.Parent = null;
        mChildren.Remove(child);
    }

    #endregion

    /// <summary>
    /// When actor was created and prepared for ticking
    /// </summary>
    protected virtual Task OnCreate() => Task.CompletedTask;

    /// <summary>
    /// When actor was removed from ticking
    /// </summary>
    protected virtual Task OnDestroy() => Task.CompletedTask;
    
    /// <summary>
    /// Actor update cycle
    /// </summary>
    protected virtual void OnTick(TimeSpan delta) { }
    
    /// <summary>
    /// When actor was removed from ticking and preparing to be moved to another cell
    /// </summary>
    protected internal virtual void OnBeforeMove() { }
    
    /// <summary>
    /// When actor was already moved to another cell and ready to tick
    /// </summary>
    protected internal virtual void OnMoved() { }

    #region Event processing

    /// <summary>
    /// Subscribe on actor event of <typeparamref name="T"/> 
    /// </summary>
    protected void Watch<T>(Action<T> eventWatcher)
    {
        if(!IsEventReceiver)
            return;
        
        var type = typeof(T).FullName ?? throw new ActorException();
        if (!mWatchers.ContainsKey(type))
            mWatchers.Add(type, eventWatcher);
        else
            mWatchers[type] = eventWatcher;
    }

    /// <summary>
    /// Send new actor event of <typeparamref name="T"/>
    /// </summary>
    protected void Emit<T>(T actorEvent, Guid[] targetIds = null) where T : class
    {
        if(actorEvent == null)
            return;

        sActorContext.Rpc.Call(RpcTopics.ACTOR_EVENT, new ActorEvent
        {
            Payload = actorEvent,
            Type = ActorEventType.Event,
            SenderActorId = Id,
            TargetActorIds = targetIds
        });
    }

    /// <summary>
    /// Put new actor event to mailbox
    /// </summary>
    internal bool ReceiveEvent(object @event)
    {
        if (!IsEventReceiver)
            return false;
        
        if (@event == null) 
            throw new ArgumentNullException(nameof(@event));

        var eventType = @event.GetType().FullName ?? throw new ActorException();
        if(!mWatchers.ContainsKey(eventType))
            return false;
        
        mMailbox.Enqueue((eventType, @event));
        return true;
    }

    /// <summary>
    /// Process all events in mailbox
    /// </summary>
    private void ProcessEvents()
    {
        var processCount = mMailbox.Count;
        for (var i = 0; i < processCount; i++)
        {
            if(!mMailbox.TryDequeue(out var actorEvent))
                continue;
            if (mWatchers.TryGetValue(actorEvent.Item1, out var watcher))
                watcher.DynamicInvoke(actorEvent.Item2);
        }
    }

    #endregion

    internal async Task InternalDestroy()
    {
        if (IsDestroyed)
            return;

        await OnDestroy();

        foreach (var t in mComponents)
            await t.Detach(true);

        IsDestroyed = true;
        
        if (!IsThin)
            await sActorContext.Rpc.Call(RpcTopics.DESTROY_EVENT, new ActorEvent
            {
                Type = ActorEventType.Delete,
                SenderActorId = Id
            });

        foreach (var child in mChildren)
            await child.InternalDestroy();

        Parent?.RemoveChild(this);
    }

    internal void InternalTick(TimeSpan delta)
    {
        if(IsDestroyed)
            return;

        if (IsEventReceiver)
            ProcessEvents();
            
        if(TickingMode is TickingMode.AllTicking or TickingMode.ActorTickingOnly)
            OnTick(delta);

        if (TickingMode is TickingMode.AllTicking or TickingMode.ComponentsTickingOnly)
        {
            for (byte i = 0; i < mComponents.Count; i++)
                if(mComponents[i].IsTicking) 
                    mComponents[i].Tick(delta);
        }
    }
    
    internal ActorEntity Serialize()
    {
        if (IsThin)
            return null;
        
        return new()
        {
            Id = Id,
            Name = Name,
            Type = GetType().AssemblyQualifiedName,
            Properties = PropertySerializer.Serialize(this),
            MechanicsId = sActorContext.Actors.MechanicsId,
            StoredAt = DateTime.UtcNow,
            WorldId = WorldId,
            ParentId = Parent?.Id,
            TickingMode = TickingMode,
            IsEventReceiver = IsEventReceiver,
            ChildrenIds = Children.Select(x => x.Id).ToArray(),
            Components = mComponents.Select(x => new ActorEntityComponent
            {
                Name = x.Name,
                Properties = PropertySerializer.Serialize(x),
                Type = x.GetType().AssemblyQualifiedName
            }).ToArray()
        };
    }

    #region Components

    /// <summary>
    /// Does actor have a component?
    /// </summary>
    public bool Has<T>(string name = null) where T : ActorComponent =>
        Get<T>(name) != null;

    internal bool Has(Type componentType, string name = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            foreach (var component in mComponents)
            {
                if (component.GetType() == componentType)
                    return true;
            }
        }
        else
        {
            foreach (var component in mComponents)
            {
                if (component.GetType() == componentType && component.Name == name)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get actor's component of <typeparamref name="T"/> with name <paramref name="name"/>
    /// </summary>
    public T Get<T>(string name = null) where T : ActorComponent
    {
        if (string.IsNullOrEmpty(name))
        {
            foreach (var component in mComponents)
            {
                if (component is T item)
                    return item;
            }
        }
        else
        {
            foreach (var component in mComponents)
            {
                if (component is T item && item.Name == name)
                    return item;
            }
        }

        return null;
    }
    
    /// <summary>
    /// Get actor's component of <param name="componentType"/> with name <paramref name="name"/>
    /// </summary>
    public ActorComponent Get(Type componentType, string name = null)
    {
        if (componentType == null) 
            throw new ArgumentNullException(nameof(componentType));
        
        if(!componentType.IsAssignableTo(typeof(ActorComponent)))
            throw new ArgumentException($"Type must be assignable to {nameof(ActorComponent)}", nameof(componentType));
        
        if (string.IsNullOrEmpty(name))
        {
            foreach (var component in mComponents)
            {
                if (component.GetType() == componentType)
                    return component;
            }
        }
        else
        {
            foreach (var component in mComponents)
            {
                if (component.GetType() == componentType && component.Name == name)
                    return component;
            }
        }

        return null;
    }

    /// <summary>
    /// Add new actor component created from DI container
    /// </summary>
    protected T Add<T>(string name = null) where T : ActorComponent
    {
        if (sActorContext.Container == null)
            throw new ActorException("Actor is not initialized");
        
        var component = sActorContext.Container.Resolve<T>(); 
        Add(component, name);
        return component;
    }

    /// <summary>
    /// Add new actor component created from DI container
    /// </summary>
    internal void Add(Type component, string name = null)
    {
        if (sActorContext.Container == null)
            throw new ActorException("Actor is not initialized");

        Add((ActorComponent) sActorContext.Container.Resolve(component), name);
    }

    /// <summary>
    /// Add already created new actor component
    /// </summary>
    protected void Add(ActorComponent component, string name = null)
    {
        AddComponentInternal(component, name);
        _ = component?.Attach();
    }

    internal void AddComponentInternal(ActorComponent component, string name = null)
    {
        if (component == null)
            return;
        
        component.Name = name;
        component.Owner = this;
        mComponents.Add(component);
    }

    protected internal void Remove(Type componentType, string name = null)
    {
        if (mComponents.Count == 0)
            return;

        int? index = null;
        if (string.IsNullOrEmpty(name))
        {
            for (var i = 0; i < mComponents.Count; i++)
            {
                if (mComponents[i].GetType() != componentType)
                    continue;

                _ = mComponents[i].Detach(false);
                index = i;
                break;
            }
        }
        else
        {
            for (byte i = 0; i < mComponents.Count; i++)
            {
                if (mComponents[i].GetType() != componentType || mComponents[i].Name != name)
                    continue;

                _ = mComponents[i].Detach(false);
                index = i;
                break;
            }
        }

        if (!index.HasValue)
            return;
        
        mComponents.RemoveAt(index.Value);
    }

    protected internal void Remove<T>(string name = null) where T : ActorComponent => Remove(typeof(T), name);

    #endregion
}