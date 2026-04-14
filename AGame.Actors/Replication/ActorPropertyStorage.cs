namespace AGame.Actors.Replication;

internal class ActorPropertyStorage : IActorProperties
{
    private record struct ActorPropStorage(Dictionary<string, object> Props);

    private readonly Dictionary<Guid, ActorPropStorage> mActorStorages = new();

    internal void Set(Guid actorId, string property, object value)
    {
        if (string.IsNullOrEmpty(property))
            throw new ArgumentException("Value cannot be null or empty.", nameof(property));

        var storage = GetActorStorage(actorId);
        
        if (storage.Props.ContainsKey(property))
            storage.Props[property] = value;
        else
            storage.Props.Add(property, value);
    }

    public bool TryGet<T>(Guid actorId, string property, out T value)
    {
        if (string.IsNullOrEmpty(property))
            throw new ArgumentException("Value cannot be null or empty.", nameof(property));

        var storage = GetActorStorage(actorId);

        if (storage.Props.TryGetValue(property, out var refValue))
        {
            value = (T) refValue;
            return true;
        }

        value = default;
        return false;
    }

    public T Get<T>(Guid actorId, string property, bool throwIfNotExists = false)
    {
        if(!TryGet<T>(actorId, property, out var value))
            return !throwIfNotExists
                ? default
                : throw new ActorException("Replication property type is missing");

        return value;
    }
    
    private ActorPropStorage GetActorStorage(Guid actorId)
    {
        if (actorId == default)
            throw new ArgumentException(nameof(actorId));
        
        if (!mActorStorages.TryGetValue(actorId, out var storage))
        {
            storage = new ActorPropStorage
            {
                Props = new Dictionary<string, object>()
            };
            mActorStorages.Add(actorId, storage);
        }

        return storage;
    }
}