using System.Collections;
using System.Linq.Expressions;
using ACore.Abstractions.Database;

namespace ACore.Tests.Shared.Database;

internal class FakeQueryable<T> : IDbQueryable<T>
{
    private IQueryable<T> mQueryable;

    public FakeQueryable(IQueryable<T> queryable)
    {
        mQueryable = queryable;
    }

    public IEnumerator<T> GetEnumerator() => mQueryable.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Type ElementType => mQueryable.ElementType;
    public Expression Expression => mQueryable.Expression;
    public IQueryProvider Provider => mQueryable.Provider;
    
    public bool All(Expression<Func<T, bool>> predicate) => mQueryable.All(predicate);

    public TAccumulate Aggregate<TAccumulate>(TAccumulate seed, Expression<Func<TAccumulate, T, TAccumulate>> func) =>
        mQueryable.Aggregate(seed, func);

    public IDbQueryable<TResult> Join<TInner, TResult>(Expression<Func<T, Guid>> keySelector, Expression<Func<T, TInner, TResult>> resultSelector) where TInner : IDbEntity => 
        new FakeQueryable<TResult>(mQueryable.Join(Array.Empty<TInner>(), keySelector, i => i.Id, resultSelector));

    public IDbQueryable<TResult> JoinGroup<TInner, TResult>(Expression<Func<T, Guid>> outerKeySelector, Expression<Func<TInner, Guid>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector) where TInner : IDbEntity =>
        new FakeQueryable<TResult>(mQueryable.GroupJoin(Array.Empty<TInner>(), outerKeySelector, innerKeySelector, resultSelector));

    public IDbQueryable<T> Skip(int count)
    {
        mQueryable = mQueryable.Skip(count);
        return this;
    }

    public IDbQueryable<T> Take(int count)
    {
        mQueryable = mQueryable.Take(count);
        return this;
    }

    public IDbQueryable<T> Where(Expression<Func<T, bool>> predicate)
    {
        mQueryable = mQueryable.Where(predicate);
        return this;
    }

    public IDbQueryable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector) => 
        new FakeQueryable<TResult>(mQueryable.Select(selector));

    public IDbQueryable<TResult> SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector) =>
        new FakeQueryable<TResult>(mQueryable.SelectMany(selector));

    public IDbOrderedQueryable<T> OrderBy<TResult>(Expression<Func<T, TResult>> selector) => new FakeOrderedQueryable<T>(mQueryable.OrderBy(selector));
    public IDbOrderedQueryable<T> OrderByDescending<TResult>(Expression<Func<T, TResult>> selector) => new FakeOrderedQueryable<T>(mQueryable.OrderByDescending(selector));

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.Any());
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.Any(predicate));
    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.Count());
    public Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.Count(predicate));
    public Task<long> LongCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.LongCount());
    public Task<long> LongCountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.LongCount(predicate));
    public Task<T> FirstAsync(CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.First());
    public Task<T> FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.First(predicate));
    public Task<T> SingleAsync(CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.Single());
    public Task<T> SingleAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.Single(predicate));
    public Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.FirstOrDefault());
    public Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.FirstOrDefault(predicate));
    public Task<T> SingleOrDefaultAsync(CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.SingleOrDefault());
    public Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.SingleOrDefault(predicate));
    public Task<T> MaxAsync(CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.Max());
    public Task<TResult> MaxAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.Max(selector));
    public Task<T> MinAsync(CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.Min());
    public Task<TResult> MinAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.Min(selector));
    public Task<List<T>> ToListAsync(CancellationToken cancellationToken = default) => Task.FromResult(mQueryable.ToList());
}

internal class FakeOrderedQueryable<T> : FakeQueryable<T>, IDbOrderedQueryable<T>
{
    private IOrderedQueryable<T> mQueryable;

    public FakeOrderedQueryable(IOrderedQueryable<T> queryable) : base(queryable)
    {
        mQueryable = queryable;
    }

    public IDbOrderedQueryable<T> ThenBy<TResult>(Expression<Func<T, TResult>> selector)
    {
        mQueryable = mQueryable.ThenBy(selector);
        return this;
    }

    public IDbOrderedQueryable<T> ThenByDescending<TResult>(Expression<Func<T, TResult>> selector)
    {
        mQueryable = mQueryable.ThenByDescending(selector);
        return this;
    }
}