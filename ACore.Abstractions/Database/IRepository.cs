using System.Linq.Expressions;

namespace ACore.Abstractions.Database;

/// <summary>
/// Common interface for CRUD operation
/// </summary>
public interface IRepository<T> where T : IDbEntity
{
    /// <summary>
    /// Get custom <see cref="IQueryable{T}"/> for query building
    /// </summary>
    IDbQueryable<T> Select();

    /// <summary>
    /// Simple single entity insert
    /// </summary>
    Task Insert(T entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Bulk insert of entity collection
    /// </summary>
    Task InsertMany(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        
    /// <summary>
    /// Delete entities by condition
    /// </summary>
    Task<long> Delete(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Simple single whole entity update
    /// </summary>
    Task Update(T entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update specified fields of single entity 
    /// </summary>
    /// <param name="id">Id of selected entity</param>
    IDbUpdateBuilder<T> Update(Guid id);

    /// <summary>
    /// Bulk update of entity collection
    /// </summary>
    Task Update(IEnumerable<T> entities, bool isUpsert = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update specified fields of entity collection selected by condition
    /// </summary>
    IDbUpdateBuilder<T> Update(Expression<Func<T, bool>> condition);
}