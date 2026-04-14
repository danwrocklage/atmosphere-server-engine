using ACore.Abstractions;

namespace AGame.Actors.Handlers;

/// <summary>
/// Map full name to type for all actors
/// </summary>
internal static class ActorTypeCache
{
    private static readonly IReadOnlyDictionary<string, Type> sTypesCache;

    static ActorTypeCache()
    {
        sTypesCache = Types.All.Where(x =>
        {
            var current = x;
            while (current.BaseType != null)
            {
                if (current.BaseType == typeof(Actor))
                    return true;

                current = current.BaseType;
            }

            return false;
        }).ToDictionary(x => x.AssemblyQualifiedName, x => x);
    }

    /// <summary>
    /// Get actor type by full name. If it doesn't exist, null will return.
    /// </summary>
    public static Type Get(string name) => sTypesCache.TryGetValue(name, out var actorType) ? actorType : null;
}