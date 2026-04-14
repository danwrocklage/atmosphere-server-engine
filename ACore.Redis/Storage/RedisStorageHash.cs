using ACore.Abstractions.Logging;
using ACore.Abstractions.Storage;
using StackExchange.Redis;

namespace ACore.Redis;

internal class RedisStorageHash : IStorageHash
{
    private readonly Lazy<string> mHashKey;
    private readonly ILogger<RedisClient> mLogger;
    private readonly Lazy<bool> mIsEnabled;
    private readonly Lazy<IDatabaseAsync> mDatabase;

    public RedisStorageHash(Func<string> hashKey, ILogger<RedisClient> logger, Func<bool> isEnabled, Func<IDatabaseAsync> database)
    {
        mHashKey = new Lazy<string>(hashKey);
        mLogger = logger;
        mIsEnabled = new Lazy<bool>(isEnabled);
        mDatabase = new Lazy<IDatabaseAsync>(database);
    }

    public async Task<bool> Exists(string key)
    {
        if (!mIsEnabled.Value) return false;
        return await mDatabase.Value.HashExistsAsync(mHashKey.Value, key);
    }

    public async Task Delete(string key)
    {
        mLogger.LogAction("Delete", $"{mHashKey.Value}:{key}");
        if (!mIsEnabled.Value) return;
        await mDatabase.Value.HashDeleteAsync(mHashKey.Value, key, CommandFlags.FireAndForget);
    }

    public async Task<string[]> GetKeys()
    {
        return !mIsEnabled.Value ? 
            Array.Empty<string>() : 
            (await mDatabase.Value.HashKeysAsync(mHashKey.Value)).ToStringArray();
    }

    public async Task Store<T>(string key, T value)
    {
        mLogger.LogAction("Set", $"{mHashKey.Value}:{key}", value);
        if (!mIsEnabled.Value) return;
        await mDatabase.Value.HashSetAsync(mHashKey.Value, key,  RedisUtils.ConvertToRedisValue(value), When.Always, CommandFlags.FireAndForget);
    }

    public async Task Store<T>(IDictionary<string, T> values)
    {
        if (!mIsEnabled.Value)
        {
            mLogger.LogAction("Set", $"{mHashKey.Value}:<values>", values);
            return;
        }
        var hashes = new HashEntry[values.Count];
        var i = 0;
        foreach (var (name, value) in values)
        {
            mLogger.LogAction("Set", $"{mHashKey.Value}:{name}", value);
            hashes[i] = new HashEntry(name, RedisUtils.ConvertToRedisValue(value));
            i++;
        }
        await mDatabase.Value.HashSetAsync(mHashKey.Value, hashes, CommandFlags.FireAndForget);
    }

    public async Task<T> Get<T>(string key)
    {
        if (!mIsEnabled.Value) return default;

        var rv = await mDatabase.Value.HashGetAsync(mHashKey.Value, key);
        return RedisUtils.ConvertToType<T>(rv);
    }

    public async Task<IEnumerable<T>> Get<T>(IEnumerable<string> keys)
    {
        if (!mIsEnabled.Value) return Enumerable.Empty<T>();

        return (await mDatabase.Value.HashGetAsync(mHashKey.Value, keys.Select(x => new RedisValue(x)).ToArray()))
            .Select(x => RedisUtils.ConvertToType<T>(x));
    }

    public async Task<IDictionary<string, T>> Get<T>()
    {
        if (!mIsEnabled.Value) return new Dictionary<string, T>();
            
        return (await mDatabase.Value.HashGetAllAsync(mHashKey.Value))
            .ToDictionary(x => x.Name.ToString(), x => RedisUtils.ConvertToType<T>(x.Value));
    }

    public async Task Increment(string key)
    {
        mLogger.LogAction("Increment", $"{mHashKey.Value}:{key}");
        if (!mIsEnabled.Value) return;
        await mDatabase.Value.HashIncrementAsync(mHashKey.Value, key, flags: CommandFlags.FireAndForget);
    }

    public async Task Decrement(string key)
    {
        mLogger.LogAction("Decrement", $"{mHashKey.Value}:{key}");
        if (!mIsEnabled.Value) return;
        await mDatabase.Value.HashDecrementAsync(mHashKey.Value, key, flags: CommandFlags.FireAndForget);
    }
}