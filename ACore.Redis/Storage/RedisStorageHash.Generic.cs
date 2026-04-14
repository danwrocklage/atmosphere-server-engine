using System.Runtime.CompilerServices;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Storage;
using StackExchange.Redis;

namespace ACore.Redis;

internal class RedisStorageHash<T> : RedisStorageHash, IStorageHash<T>
{
    public RedisStorageHash(Func<string> hashKey, ILogger<RedisClient> logger, Func<bool> isEnabled, Func<IDatabaseAsync> database) : 
        base(hashKey, logger, isEnabled, database)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task Store(string key, T value) => Store<T>(key, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task Store(IDictionary<string, T> values) => Store<T>(values);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<T> Get(string key) => Get<T>(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<IEnumerable<T>> Get(IEnumerable<string> keys) => Get<T>(keys);

    public Task<IDictionary<string, T>> Get() => Get<T>();
}