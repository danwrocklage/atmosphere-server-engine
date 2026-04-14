using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ACore.Abstractions;
using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace ACore.MongoDb;

/// <inheritdoc cref="IDatabase" />
[Log(Category = "[db] mongo")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal class MongoDatabase : IDatabase, IAsyncInitializable
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private MongoClient mClient;
    private IMongoDatabase mDatabase;
    private readonly ILogger<MongoDatabase> mLogger;
    private readonly IConfiguration mConfiguration;
    private readonly string mCellName;
    private readonly ConcurrentDictionary<Type, object> mCachedRepositories;

    static MongoDatabase()
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
    }

    public MongoDatabase(ILogger<MongoDatabase> logger, ICellEnvironment cellInformation, IConfiguration configuration)
    {
        mLogger = logger;
        mConfiguration = configuration;
        mCellName = cellInformation.ToString();
        mCachedRepositories = new ConcurrentDictionary<Type, object>();
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var config = mConfiguration.Get(() => DatabaseConfig.Default);
        mLogger.Debug($"Connecting to [{config.ConnectionString}]...");
            
        try
        {
            var connection = new MongoUrl(config.ConnectionString);
            var settings = MongoClientSettings.FromUrl(connection);
            settings.ConnectTimeout = TimeSpan.FromSeconds(config.Timeout);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(config.Timeout);
            settings.ApplicationName = mCellName;

            mClient = new MongoClient(settings);
            mDatabase = mClient.GetDatabase(connection.DatabaseName);

            // Checking connection
            (await mClient.StartSessionAsync()).Dispose();

            mLogger.Success($"Connected to [{config.ConnectionString}]");
        }
        catch (TimeoutException)
        {
            mLogger.Error("Mongodb server is unavailable");
            mDatabase = null;
        }
        catch (Exception e)
        {
            mLogger.Error("Unknown error", e);
            mDatabase = null;
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IRepository<T> Repository<T>() where T : IDbEntity => 
        GetOrCreateRepository(() => new MongoRepository<T>(() => mDatabase, mLogger));

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IRepository<T> Repository<T>(string name) where T : IDbEntity
    {
        if (name == null) 
            throw new ArgumentNullException(nameof(name));

        return GetOrCreateRepository(() => new MongoRepository<T>(() => mDatabase, mLogger, name));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IRepository<T> GetOrCreateRepository<T>(Func<IRepository<T>> creator) where T : IDbEntity
    {
        //if(!mIsInitialized)
        //    throw new InvalidOperationException("Database provider must be initialized before create repositories");
            
        var type = typeof(T);
        if (mCachedRepositories.TryGetValue(type, out var repository))
            return (IRepository<T>) repository;
            
        repository = creator();
            
        mCachedRepositories.TryAdd(type, repository);
        return (IRepository<T>) repository;
    }

    #region Util

    [Configuration("db.mongo")]
    private class DatabaseConfig
    {
        public string ConnectionString { get; set; }

        public int Timeout { get; set; }
            
        public static DatabaseConfig Default => new()
        {
            // ReSharper disable once StringLiteralTypo
            ConnectionString = "mongodb://localhost:27017/acore",
            Timeout = 3
        };
    }

    #endregion
}