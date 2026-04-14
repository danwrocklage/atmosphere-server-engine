using System.Runtime.CompilerServices;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Storage;
using StackExchange.Redis;

namespace ACore.Redis;

internal class RedisStorageList<T> : RedisStorageList, IStorageList<T>
{
    public RedisStorageList(Func<string> listKey, ILogger<RedisClient> logger, Func<bool> isEnabled, Func<IDatabaseAsync> database) : 
        base(listKey, logger, isEnabled, database)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<bool> Exists(T value) => Exists<T>(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task Delete(T value) => Delete<T>(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task Store(T value) => Store<T>(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task Store(IList<T> values) => Store<T>(values);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<IEnumerable<T>> GetAll() => GetAll<T>();
}