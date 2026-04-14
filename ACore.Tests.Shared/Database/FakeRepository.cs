using System.Linq.Expressions;
using ACore.Abstractions.Database;

namespace ACore.Tests.Shared.Database;

public class FakeRepository<T> : IRepository<T> where T : IDbEntity
{
    public List<T> RawData { get; set; } = new ();

    public IDbQueryable<T> Select() => new FakeQueryable<T>(RawData.AsQueryable());

    public Task Insert(T entity, CancellationToken cancellationToken = default)
    {
        if (entity == null) 
            throw new ArgumentNullException(nameof(entity));
        RawData.Add(entity);
        return Task.CompletedTask;
    }

    public Task InsertMany(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        if (entities == null) 
            throw new ArgumentNullException(nameof(entities));
        
        RawData.AddRange(entities);
        return Task.CompletedTask;
    }

    public Task<long> Delete(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
    {
        if (filter == null) throw new ArgumentNullException(nameof(filter));
        var selected = RawData.AsQueryable().Where(filter).ToArray();
        foreach (var entity in selected)
            RawData.Remove(entity);
        return Task.FromResult<long>(selected.Length);
    }

    public Task Update(T entity, CancellationToken cancellationToken = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        var index = RawData.FindIndex(x => x.Id == entity.Id);
        RawData[index] = entity;
        return Task.CompletedTask;
    }

    public IDbUpdateBuilder<T> Update(Guid id) => Update(x => x.Id == id);

    public async Task Update(IEnumerable<T> entities, bool isUpsert = false, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            await Update(entity, cancellationToken);
        }
    }

    public IDbUpdateBuilder<T> Update(Expression<Func<T, bool>> condition)
    {
        var entity = RawData.Where(condition.Compile()).ToArray();
        return new FakeDbUpdateBuilder<T>(entity);
    }
}