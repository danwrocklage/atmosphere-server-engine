namespace ACore.Abstractions.Database;

/// <summary>
/// Database common interface
/// </summary>
public interface IDatabase
{
    /// <summary>
    /// Shorthand for <see cref="Repository{T}()"/>.<see cref="IRepository{T}.Select()"/>
    /// </summary>
    public IDbQueryable<T> Select<T>() where T : IDbEntity => Repository<T>().Select();
        
    /// <summary>
    /// Get repository for entity of <typeparamref name="T"/>
    /// </summary>
    IRepository<T> Repository<T>() where T : IDbEntity;
      
    /// <summary>
    /// Get repository for entity of <typeparamref name="T"/> with special name in DB
    /// </summary>
    IRepository<T> Repository<T>(string name) where T : IDbEntity;
}