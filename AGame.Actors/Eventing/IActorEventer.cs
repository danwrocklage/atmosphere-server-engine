namespace AGame.Actors.Eventing;

/// <summary>
/// Receiver global actor events
/// </summary>
public interface IActorEventer
{
    /// <summary>
    /// When new actor was created (not thin)
    /// </summary>
    event Action<Guid> CreateActor;

    /// <summary>
    /// When actor was destroyed
    /// </summary>
    event Action<Guid> DestroyActor;
}