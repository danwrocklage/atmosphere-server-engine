using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Text.Json;
using ACore.Abstractions.Logging;

namespace ACore.Abstractions.Database;

internal class EmptyUpdateBuilder<TEntity> : IDbUpdateBuilder<TEntity> where TEntity : IDbEntity
{
    public IDbUpdateBuilder<TEntity> Set<TField>(Expression<Func<TEntity, TField>> expression, TField value) =>
        this;

    public IDbUpdateBuilder<TEntity> Inc<TField>(Expression<Func<TEntity, TField>> expression, TField value)
        where TField : struct =>
        this;

    public IDbUpdateBuilder<TEntity> Push<TField>(Expression<Func<TEntity, IEnumerable<TField>>> expression,
        TField value) =>
        this;

    public Task<long> Apply(bool isUpsert = false, CancellationToken token = default) => Task.FromResult<long>(default);
}

[Log(Category = "[db] null")]
public class EmptyRepository<T> : IRepository<T> where T : IDbEntity
{
    private readonly ILogger<EmptyRepository<T>> mLogger;
    private readonly string mName;

    public EmptyRepository(ILogger<EmptyRepository<T>> logger)
    {
        mLogger = logger;
        var type = typeof(T);
        mName = ((TableAttribute) type.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault())
            ?.Name ?? type.Name;
    }
        
    public IDbQueryable<T> Select() => new DefaultQueryableProxy<T>(new EnumerableQuery<T>(new List<T>()));

    public Task Insert(T entity, CancellationToken cancellationToken = default)
    {
        mLogger.Debug($"[Disabled][{mName}] Insert {JsonSerializer.Serialize(entity)}");
        return Task.CompletedTask;
    }

    public Task InsertMany(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        mLogger.Debug($"[Disabled][{mName}] Insert many {JsonSerializer.Serialize(entities)}");
        return Task.CompletedTask;
    }

    public Task<long> Delete(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
    {
        mLogger.Debug($"[Disabled][{mName}] Delete");
        return Task.FromResult<long>(default);
    }

    public Task Update(T entity, CancellationToken cancellationToken = default)
    {
        mLogger.Debug($"[Disabled][{mName}] Update {JsonSerializer.Serialize(entity)}");
        return Task.CompletedTask;
    }

    public IDbUpdateBuilder<T> Update(Guid id)
    {
        mLogger.Debug($"[Disabled][{mName}] Update {id} fields");
        return new EmptyUpdateBuilder<T>();
    }

    public Task Update(IEnumerable<T> entities, bool isUpsert = false, CancellationToken cancellationToken = default)
    {
        mLogger.Debug($"[Disabled][{mName}] Update many {JsonSerializer.Serialize(entities)}");
        return Task.CompletedTask;
    }

    public IDbUpdateBuilder<T> Update(Expression<Func<T, bool>> condition)
    {
        mLogger.Debug($"[Disabled][{mName}] Update fields by condition");
        return new EmptyUpdateBuilder<T>();
    }
}