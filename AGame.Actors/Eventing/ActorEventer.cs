namespace AGame.Actors.Eventing;

/// <inheritdoc />
internal class ActorEventer : IActorEventer
{
    /// <inheritdoc />
    public event Action<Guid> CreateActor;

    /// <inheritdoc />
    public event Action<Guid> DestroyActor;

    /// <summary>
    /// Invoke <see cref="CreateActor"/> event
    /// </summary>
    public virtual void OnCreateActor(Guid actorId)
    {
        CreateActor?.Invoke(actorId);
    }

    /// <summary>
    /// Invoke <see cref="DestroyActor"/> event
    /// </summary>
    public virtual void OnDestroyActor(Guid actorId)
    {
        DestroyActor?.Invoke(actorId);
    }
}