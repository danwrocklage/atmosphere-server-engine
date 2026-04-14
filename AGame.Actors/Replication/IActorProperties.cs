namespace AGame.Actors.Replication;

public interface IActorProperties
{
    bool TryGet<T>(Guid actorId, string property, out T value);
    T Get<T>(Guid actorId, string property, bool throwIfNotExists = false);
}