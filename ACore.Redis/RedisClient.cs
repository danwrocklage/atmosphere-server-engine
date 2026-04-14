using ACore.Abstractions;
using ACore.Abstractions.Extensions;
using ACore.Abstractions.Logging;
using StackExchange.Redis;

namespace ACore.Redis;

/// <summary>
/// Engine level Redis client
/// </summary>
[Log(Category = "[st] Redis")]
public class RedisClient : IInitializable, IDisposable
{
    private readonly IConfiguration mConfig;
    private readonly ILogger<RedisClient> mLogger;
        
    public RedisClient(IConfiguration config, ILogger<RedisClient> logger)
    {
        mConfig = config;
        mLogger = logger;
    }
        
    public IDatabase Database { get; private set; }
        
    public string Namespace { get; private set; }

    public bool IsEnabled { get; private set; }

    internal event Action<IDatabase> OnDatabaseChanged;
        
    public void Initialize()
    {
        var redisConfig = mConfig.Get(() => RedisConfig.Default);
        Namespace = redisConfig.Namespace;
            
        try
        {
            var connection = ConnectionMultiplexer.Connect(redisConfig.ConnectionString);

            connection.ErrorMessage += (_, args) => { mLogger.Error(args.Message); };
            connection.ConnectionFailed += (_, args) => 
            {
                mLogger.Error($"Failed to connect to {args.EndPoint}");
                mLogger.Debug(args.Exception.GetFullMessage());
                IsEnabled = false;
            };
            connection.ConnectionRestored += (_, _) => 
            {
                mLogger.Info("Connection restored");
                IsEnabled = true;
            };
            connection.InternalError += (_, args) => { mLogger.Debug($"Internal error ({args.Origin})", args.Exception); };
            connection.ErrorMessage += (_, args) => { mLogger.Error($"Redis server error '{args.Message}'"); };
            
            Database = connection.GetDatabase();
            OnDatabaseChanged?.Invoke(Database);

            IsEnabled = true;
            mLogger.Success($"Connected to [{redisConfig.ConnectionString}]");
        }
        catch (Exception ex)
        {
            mLogger.Error($"Unable to connect redis {redisConfig.ConnectionString}", ex);
            IsEnabled = false;
            Database = null;
        }
    }

    public void Dispose()
    {
        if (!IsEnabled) return;

        Database.Multiplexer.Dispose();
        mLogger.Debug("Disposed");
    }

    #region Utils

    [Configuration("storage.redis")]
    private class RedisConfig
    {
        public string ConnectionString { get; set; }
            
        public string Namespace { get; set; }

        public static RedisConfig Default => new RedisConfig
        {
            ConnectionString = "localhost:6379",
            Namespace = "a"
        };
    }

    #endregion
}