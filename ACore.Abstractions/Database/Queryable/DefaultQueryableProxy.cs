using System.Collections;
using System.Linq.Expressions;

namespace ACore.Abstractions.Database;

internal class DefaultQueryableProxy<T> : IDbQueryable<T>
{
    protected IQueryable<T> Queryable;

    public DefaultQueryableProxy(IQueryable<T> queryable)
    {
        Queryable = queryable;
    }

    public IEnumerator<T> GetEnumerator() => Queryable.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Type ElementType => Queryable.ElementType;
    public Expression Expression => Queryable.Expression;
    public IQueryProvider Provider => Queryable.Provider;

    public bool All(Expression<Func<T, bool>> predicate) => Queryable.All(predicate);

    public TAccumulate
        Aggregate<TAccumulate>(TAccumulate seed, Expression<Func<TAccumulate, T, TAccumulate>> func) =>
        Queryable.Aggregate(seed, func);

    public IDbQueryable<TResult> Join<TInner, TResult>(Expression<Func<T, Guid>> keySelector, Expression<Func<T, TInner, TResult>> resultSelector) where TInner : IDbEntity =>
        new DefaultQueryableProxy<TResult>(Queryable.Join(new List<TInner>(), keySelector, x => x.Id,
            resultSelector));

    public IDbQueryable<TResult> JoinGroup<TInner, TResult>(Expression<Func<T, Guid>> outerKeySelector, Expression<Func<TInner, Guid>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector) where TInner : IDbEntity =>
        new DefaultQueryableProxy<TResult>(Queryable.GroupJoin(new List<TInner>(), outerKeySelector, innerKeySelector, resultSelector));

    public IDbQueryable<T> Skip(int count)
    {
        Queryable = Queryable.Skip(count);
        return this;
    }

    public IDbQueryable<T> Take(int count)
    {
        Queryable = Queryable.Take(count);
        return this;
    }

    public IDbQueryable<T> Where(Expression<Func<T, bool>> predicate)
    {
        Queryable = Queryable.Where(predicate);
        return this;
    }

    public IDbQueryable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector) =>
        new DefaultQueryableProxy<TResult>(Queryable.Select(selector));

    public IDbQueryable<TResult> SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector) =>
        new DefaultQueryableProxy<TResult>(Queryable.SelectMany(selector));

    public IDbOrderedQueryable<T> OrderBy<TResult>(Expression<Func<T, TResult>> selector) =>
        new DefaultOrderedQueryableProxy<T>(Queryable.OrderBy(selector));

    public IDbOrderedQueryable<T> OrderByDescending<TResult>(Expression<Func<T, TResult>> selector) =>
        new DefaultOrderedQueryableProxy<T>(Queryable.OrderByDescending(selector));

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => Task.FromResult<bool>(default);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<bool>(default);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult<int>(default);

    public Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<int>(default);

    public Task<long> LongCountAsync(CancellationToken cancellationToken = default) => Task.FromResult<long>(default);

    public Task<long> LongCountAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<long>(default);

    public Task<T> FirstAsync(CancellationToken cancellationToken = default) => Task.FromResult<T>(default);

    public Task<T> FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<T>(default);

    public Task<T> SingleAsync(CancellationToken cancellationToken = default) => Task.FromResult<T>(default);

    public Task<T> SingleAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<T>(default);

    public Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default) => Task.FromResult<T>(default);

    public Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) => Task.FromResult<T>(default);

    public Task<T> SingleOrDefaultAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<T>(default);

    public Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) => Task.FromResult<T>(default);

    public Task<T> MaxAsync(CancellationToken cancellationToken = default) => Task.FromResult<T>(default);

    public Task<TResult> MaxAsync<TResult>(Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default) => Task.FromResult<TResult>(default);

    public Task<T> MinAsync(CancellationToken cancellationToken = default) => Task.FromResult<T>(default);

    public Task<TResult> MinAsync<TResult>(Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default) => Task.FromResult<TResult>(default);

    public Task<List<T>> ToListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new List<T>());
}

internal class DefaultOrderedQueryableProxy<T> : DefaultQueryableProxy<T>, IDbOrderedQueryable<T>
{
    public IDbOrderedQueryable<T> ThenBy<TResult>(Expression<Func<T, TResult>> selector)
    {
        Queryable = ((IOrderedQueryable<T>) Queryable).ThenBy(selector);
        return this;
    }

    public IDbOrderedQueryable<T> ThenByDescending<TResult>(Expression<Func<T, TResult>> selector)
    {
        Queryable = ((IOrderedQueryable<T>) Queryable).ThenByDescending(selector);
        return this;
    }

    public DefaultOrderedQueryableProxy(IOrderedQueryable<T> queryable) : base(queryable)
    {
    }
}