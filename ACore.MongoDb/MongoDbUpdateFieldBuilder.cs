using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace ACore.MongoDb;

/// <inheritdoc />
internal class MongoDbUpdateFieldBuilder<TEntity> : IDbUpdateBuilder<TEntity> where TEntity : IDbEntity
{
    private readonly IMongoCollection<TEntity> mCollection;
    private readonly BsonDocument mUpdateDocument;
    private readonly FilterDefinition<TEntity> mSelector;
    private readonly ILogger<MongoDatabase> mLogger;

    public MongoDbUpdateFieldBuilder(IMongoCollection<TEntity> collection, 
        ILogger<MongoDatabase> logger, FilterDefinition<TEntity> selector)
    {
        mCollection = collection;
        mLogger = logger;
        mSelector = selector;
        mUpdateDocument = new BsonDocument();
    }
    
    /// <inheritdoc />
    public IDbUpdateBuilder<TEntity> Set<TField>(Expression<Func<TEntity, TField>> expression, TField value)
    {
        if (expression.Body is not MemberExpression memberExpression)
            throw new ArgumentException("Only member accessing is supported", $"{nameof(expression)}");

        AddIfNotExists("$set");
        
        if(value == null)
            mUpdateDocument["$set"].AsBsonDocument.Add(memberExpression.Member.Name, BsonNull.Value);
        else
            mUpdateDocument["$set"].AsBsonDocument
                .Add(memberExpression.Member.Name, typeof(TField).IsValueType ? 
                    BsonSerializer.LookupSerializer<TField>().ToBsonValue(value) : 
                    value.ToBsonDocument());
        
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddIfNotExists(string field)
    {
        if (field == null) 
            throw new ArgumentNullException(nameof(field));
        
        if (!mUpdateDocument.Contains(field))
            mUpdateDocument.Add(field, new BsonDocument());
    }

    /// <inheritdoc />
    public IDbUpdateBuilder<TEntity> Inc<TField>(Expression<Func<TEntity, TField>> expression, TField value) where TField : struct
    {
        if (expression.Body is not MemberExpression memberExpression)
            throw new ArgumentException("Only member accessing is supported", $"{nameof(expression)}");

        AddIfNotExists("$inc");

        mUpdateDocument["$inc"].AsBsonDocument
            .Add(memberExpression.Member.Name, typeof(TField).IsValueType ? 
                BsonSerializer.LookupSerializer<TField>().ToBsonValue(value) : 
                value.ToBsonDocument());
        
        return this;
    }

    public IDbUpdateBuilder<TEntity> Push<TField>(Expression<Func<TEntity, IEnumerable<TField>>> expression, TField value)
    {
        if (expression.Body is not MemberExpression memberExpression)
            throw new ArgumentException("Only member accessing is supported", $"{nameof(expression)}");
        
        AddIfNotExists("$push");

        if(value == null)
            mUpdateDocument["$push"].AsBsonDocument.Add(memberExpression.Member.Name, BsonNull.Value);
        else
            mUpdateDocument["$push"].AsBsonDocument
                .Add(memberExpression.Member.Name, typeof(TField).IsValueType ? 
                    BsonSerializer.LookupSerializer<TField>().ToBsonValue(value) : 
                    value.ToBsonDocument());
        return this;
    }

    /// <inheritdoc />
    public async Task<long> Apply(bool isUpsert = false, CancellationToken token = default)
    {
        var result = await mCollection.UpdateManyAsync(mSelector, mUpdateDocument, new UpdateOptions {IsUpsert = isUpsert}, cancellationToken: token);
        if (result.IsAcknowledged) 
            return result.ModifiedCount;
        
        mLogger.Debug("Something went wrong while update item");
        return default;
    }
}