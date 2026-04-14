using System.Linq.Expressions;

namespace ACore.Abstractions.Database;

public interface IDbQueryable<TSource> : IOrderedQueryable<TSource>
{
    bool All(Expression<Func<TSource, bool>> predicate);
    TAccumulate Aggregate<TAccumulate>(TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func);

    public IDbQueryable<TResult> Join<TInner, TResult>(Expression<Func<TSource, Guid>> keySelector,
        Expression<Func<TSource, TInner, TResult>> resultSelector) where TInner : IDbEntity;

    public IDbQueryable<TResult> JoinGroup<TInner, TResult>(Expression<Func<TSource, Guid>> outerKeySelector,
        Expression<Func<TInner, Guid>> innerKeySelector,
        Expression<Func<TSource, IEnumerable<TInner>, TResult>> resultSelector) where TInner : IDbEntity;

    IDbQueryable<TSource> Skip(int count);
    IDbQueryable<TSource> Take(int count);
    IDbQueryable<TSource> Where(Expression<Func<TSource, bool>> predicate);
    IDbQueryable<TResult> Select<TResult>(Expression<Func<TSource, TResult>> selector);
    IDbQueryable<TResult> SelectMany<TResult>(Expression<Func<TSource, IEnumerable<TResult>>> selector);
        
    IDbOrderedQueryable<TSource> OrderBy<TResult>(Expression<Func<TSource, TResult>> selector);
    IDbOrderedQueryable<TSource> OrderByDescending<TResult>(Expression<Func<TSource, TResult>> selector);
        
    //GroupBy
    //Join
    //Distinct
    //Union
    //Zip
    //Intersect
        
    // Last & LastOrDefault
    // ElementAt & ElementAtOrDefault
    // SkipLast & SkipWhile
    // TakeLast & TakeWhile

    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default);
        
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default);
        
    Task<long> LongCountAsync(CancellationToken cancellationToken = default);
    Task<long> LongCountAsync(Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default);
        
    Task<TSource> FirstAsync(CancellationToken cancellationToken = default);
    Task<TSource> FirstAsync(Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default);
        
    Task<TSource> SingleAsync(CancellationToken cancellationToken = default);
    Task<TSource> SingleAsync(Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default);
        
    Task<TSource> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
    Task<TSource> FirstOrDefaultAsync(Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default);
        
    Task<TSource> SingleOrDefaultAsync(CancellationToken cancellationToken = default);
    Task<TSource> SingleOrDefaultAsync(Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default);
        
    Task<TSource> MaxAsync(CancellationToken cancellationToken = default);
    Task<TResult> MaxAsync<TResult>(Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default);
        
    Task<TSource> MinAsync(CancellationToken cancellationToken = default);
    Task<TResult> MinAsync<TResult>(Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default);
        
    // Average
    // Sum
        
    Task<List<TSource>> ToListAsync(CancellationToken cancellationToken = default);
}