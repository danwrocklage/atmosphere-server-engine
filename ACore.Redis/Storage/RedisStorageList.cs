using ACore.Abstractions.Logging;
using ACore.Abstractions.Storage;
using StackExchange.Redis;

namespace ACore.Redis;

internal class RedisStorageList : IStorageList
{
    private readonly Lazy<string> mListName;
    private readonly ILogger<RedisClient> mLogger;
    private readonly Lazy<bool> mIsEnabled;
    private readonly Lazy<IDatabaseAsync> mDatabase;

    public RedisStorageList(Func<string> listName, ILogger<RedisClient> logger, Func<bool> isEnabled, Func<IDatabaseAsync> database)
    {
        mListName = new Lazy<string>(listName);
        mIsEnabled = new Lazy<bool>(isEnabled);
        mDatabase = new Lazy<IDatabaseAsync>(database);
        mLogger = logger;
    }

    public async Task<long> Count()
    {
        if (!mIsEnabled.Value) return 0;
        return await mDatabase.Value.SetLengthAsync(mListName.Value);
    }

    public async Task<bool> Exists<T>(T value)
    {
        if (!mIsEnabled.Value) return false;
        return await mDatabase.Value.SetContainsAsync(mListName.Value, RedisUtils.ConvertToRedisValue(value));
    }

    public async Task Delete<T>(T value)
    {
        mLogger.LogAction("Delete", $"{mListName}", value);
        if (!mIsEnabled.Value) return;
        await mDatabase.Value.SetRemoveAsync(mListName.Value, RedisUtils.ConvertToRedisValue(value), CommandFlags.FireAndForget);
    }

    public async Task Store<T>(T value)
    {
        mLogger.LogAction("Set", $"{mListName}", value);
        if (!mIsEnabled.Value) return;
        await mDatabase.Value.SetAddAsync(mListName.Value, RedisUtils.ConvertToRedisValue(value), CommandFlags.FireAndForget);
    }

    public async Task Store<T>(IList<T> values)
    {
        if (!mIsEnabled.Value)
        {
            mLogger.LogAction("Set", $"{mListName.Value}", values);
            return;
        }
        var hashes = new RedisValue[values.Count];
        var i = 0;
        foreach (var value in values)
        {
            mLogger.LogAction("Set", $"{mListName.Value}", value);
            hashes[i] = new RedisValue(RedisUtils.ConvertToRedisValue(value));
            i++;
        }
        await mDatabase.Value.SetAddAsync(mListName.Value, hashes, CommandFlags.FireAndForget);
    }

    public async Task<IEnumerable<T>> GetAll<T>()
    {
        if (!mIsEnabled.Value) return default;
            
        return (await mDatabase.Value.SetMembersAsync(mListName.Value))
            .Select(x => RedisUtils.ConvertToType<T>(x));
    }
}