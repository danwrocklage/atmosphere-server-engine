using System.Linq.Expressions;

namespace ACore.Abstractions.Database;

/// <summary>
/// Entity field updater
/// </summary>
public interface IDbUpdateBuilder<TEntity>  where TEntity : IDbEntity
{
    /// <summary>
    /// Update field with value
    /// </summary>
    /// <param name="expression">Field selector expression</param>
    /// <param name="value">New value for field</param>
    IDbUpdateBuilder<TEntity> Set<TField>(Expression<Func<TEntity, TField>> expression, TField value);

    /// <summary>
    /// Increment field value
    /// </summary>
    IDbUpdateBuilder<TEntity> Inc<TField>(Expression<Func<TEntity, TField>> expression, TField value)
        where TField : struct;

    /// <summary>
    /// Add a value to an array
    /// </summary>
    IDbUpdateBuilder<TEntity> Push<TField>(Expression<Func<TEntity, IEnumerable<TField>>> expression, TField value);

    /// <summary>
    /// Apply update operation
    /// </summary>
    Task<long> Apply(bool isUpsert = false, CancellationToken token = default);
}