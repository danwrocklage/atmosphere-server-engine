using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ACore.MongoDb;

internal static class MongoCollectionExtensions
{
    public static IMongoCollection<T> GetCollection<T>(this IMongoDatabase database)
    {
        var type = typeof(T);
        var name = ((TableAttribute) type.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault())
            ?.Name ?? type.Name;

        return database.GetCollection<T>(name);
    }
}

/// <inheritdoc />
internal class MongoRepository<T> : IRepository<T> where T : IDbEntity
{
    private readonly Lazy<IMongoCollection<T>> mCollection;
    private readonly Lazy<IMongoDatabase> mDatabase;
    private readonly ILogger<MongoDatabase> mLogger;

    public MongoRepository(Func<IMongoDatabase> database, ILogger<MongoDatabase> logger)
    {
        mDatabase = new Lazy<IMongoDatabase>(database);
        mLogger = logger;
        mCollection = new Lazy<IMongoCollection<T>>(() => mDatabase.Value.GetCollection<T>());
    }

    public MongoRepository(Func<IMongoDatabase> database, ILogger<MongoDatabase> logger, string name)
    {
        mLogger = logger;
        mDatabase = new Lazy<IMongoDatabase>(database);
        mCollection = new Lazy<IMongoCollection<T>>(() => mDatabase.Value.GetCollection<T>(name));
    }

    /// <inheritdoc />
    public IDbQueryable<T> Select() => new MongoQueryableProxy<T>(mCollection.Value.AsQueryable(), mDatabase.Value);

    /// <inheritdoc />
    public Task Insert(T entity, CancellationToken cancellationToken = default) => mCollection.Value.InsertOneAsync(entity, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public Task InsertMany(IEnumerable<T> entities, CancellationToken cancellationToken = default) => mCollection.Value.InsertManyAsync(entities, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public async Task<long> Delete(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
    {
        var result = await mCollection.Value.DeleteManyAsync(filter, cancellationToken);
        if (result.IsAcknowledged) 
            return result.DeletedCount;
        
        mLogger.Debug("Something went wrong while delete item");
        return default;
    }

    /// <inheritdoc />
    public async Task Update(T entity, CancellationToken cancellationToken = default)
    {
        var idSelector = new BsonDocument("_id", entity.Id.ToString());
        var result = await mCollection.Value.UpdateOneAsync(idSelector, new BsonDocument("$set", entity.ToBsonDocument()), cancellationToken: cancellationToken);
        if (!result.IsAcknowledged || result.ModifiedCount != 1)
            mLogger.Debug("Something went wrong while update item");
    }

    /// <inheritdoc />
    public IDbUpdateBuilder<T> Update(Guid id) =>
        new MongoDbUpdateFieldBuilder<T>(mCollection.Value, mLogger, Builders<T>.Filter.Eq(x => x.Id, id));

    /// <inheritdoc />
    public IDbUpdateBuilder<T> Update(Expression<Func<T, bool>> condition) =>
        new MongoDbUpdateFieldBuilder<T>(mCollection.Value, mLogger, Builders<T>.Filter.Where(condition));

    /// <inheritdoc />
    public async Task Update(IEnumerable<T> entities, bool isUpsert = false, CancellationToken cancellationToken = default)
    {
        var updateOneModels = entities
            .Select(x => !isUpsert ? 
                new UpdateOneModel<T>(new ExpressionFilterDefinition<T>(e => e.Id == x.Id), x.ToBsonDocument()) : 
                (WriteModel<T>) new ReplaceOneModel<T>(new ExpressionFilterDefinition<T>(e => e.Id == x.Id), x) {IsUpsert = true})
            .ToList();
        var result = await mCollection.Value.BulkWriteAsync(updateOneModels, cancellationToken: cancellationToken);
        if (!result.IsAcknowledged || result.ModifiedCount != updateOneModels.Count)
            mLogger.Debug("Something went wrong while update many items");
    }
}