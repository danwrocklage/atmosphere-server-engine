using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Storage;
using StackExchange.Redis;

namespace ACore.Redis;

// ReSharper disable once ClassNeverInstantiated.Global
internal class RedisStorage : IStorage
{
    private readonly ILogger<RedisClient> mLogger;
    private readonly RedisClient mClient;
    private IDatabaseAsync mDatabase;

    private readonly ConcurrentDictionary<string, object> mCollectionsObjects;

    public RedisStorage(ILogger<RedisClient> logger, RedisClient client)
    {
        mLogger = logger;
        mClient = client;
        mCollectionsObjects = new ConcurrentDictionary<string, object>();
        mClient.OnDatabaseChanged += 
            database => mDatabase = database;
    }

    private RedisStorage(RedisClient redisClient, ILogger<RedisClient> logger, IDatabaseAsync database,
        ConcurrentDictionary<string, object> collectionsObjects)
    {
        mLogger = logger;
        mDatabase = database;
        mCollectionsObjects = collectionsObjects;
        mClient = redisClient;
    }

    public async Task Transaction(Func<IStorage, Task> transaction)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (mClient == null)
            throw new InvalidOperationException($"Call {nameof(Transaction)} in {nameof(Transaction)} is forbidden");
        var transactionDatabase = mClient.Database.CreateTransaction();
        await transaction(new RedisStorage(mClient, mLogger, transactionDatabase, mCollectionsObjects));
        if(!await transactionDatabase.ExecuteAsync())
            mLogger.Warn("Transaction failed");
    }

    public async Task Store<T>(string key, T value, TimeSpan expire)
    {
        if (!mClient.IsEnabled) return;
        var nsKey = NsKey(key);
        mLogger.LogAction("Set", nsKey, value);
        await mDatabase.StringSetAsync(nsKey, RedisUtils.ConvertToRedisValue(value), expire, When.Always, CommandFlags.FireAndForget);
    }

    public async Task Store<T>(string key, T value)
    {
        var nsKey = NsKey(key);
        mLogger.LogAction("Set", nsKey, value);
        if (!mClient.IsEnabled) return;
        await mDatabase.StringSetAsync(nsKey, RedisUtils.ConvertToRedisValue(value), null, When.Always, CommandFlags.FireAndForget);
    }

    public IStorageHash<THash> HashOf<THash>(string key) =>
        GetOrCreateCollection(key, () => new RedisStorageHash<THash>(() => NsKey(key), mLogger, () => mClient.IsEnabled, () => mDatabase));

    public IStorageList<TItem> ListOf<TItem>(string key) => 
        GetOrCreateCollection(key, () => new RedisStorageList<TItem>(() => NsKey(key), mLogger, () => mClient.IsEnabled, () => mDatabase));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T GetOrCreateCollection<T>(string key, Func<T> creator) where T : class
    {
        if (mCollectionsObjects.TryGetValue(NsKey(key), out var hash)) 
            return (T) hash;
            
        hash = creator();
        mCollectionsObjects.TryAdd(NsKey(key), hash);
        return (T) hash;
    }

    public async Task<T> Get<T>(string key)
    {
        if (!mClient.IsEnabled) return default;

        var rv = await mDatabase.StringGetAsync(NsKey(key));
        return RedisUtils.ConvertToType<T>(rv);
    }

    public async Task<bool> Exists(string key)
    {
        if (!mClient.IsEnabled) return false;
        return await mDatabase.KeyExistsAsync(NsKey(key));
    }

    public async Task Delete(string key)
    {
        var nsKey = NsKey(key);
        mLogger.LogAction("Delete", nsKey);
        if (!mClient.IsEnabled) return;
        await mDatabase.KeyDeleteAsync(nsKey, CommandFlags.FireAndForget);
    }

    public async Task Increment(string key)
    {
        var nsKey = NsKey(key);
        mLogger.LogAction("Increment", nsKey);
        if (!mClient.IsEnabled) return;
        await mDatabase.StringIncrementAsync(nsKey, 1, CommandFlags.FireAndForget);
    }

    public async Task Decrement(string key)
    {
        var nsKey = NsKey(key);
        mLogger.LogAction("Decrement", nsKey);
        if (!mClient.IsEnabled) return;
        await mDatabase.StringDecrementAsync(nsKey, 1, CommandFlags.FireAndForget);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string NsKey(string key) => $"{mClient.Namespace}:{key}";
}