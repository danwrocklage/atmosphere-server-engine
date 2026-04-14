using System.Collections;
using System.Linq.Expressions;
using ACore.Abstractions.Database;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ACore.MongoDb;

internal class MongoQueryableProxy<T> : IDbQueryable<T>
{
    protected IMongoQueryable<T> Queryable;
    private readonly IMongoDatabase mMongoDatabase;

    public MongoQueryableProxy(IMongoQueryable<T> queryable, IMongoDatabase mongoDatabase)
    {
        Queryable = queryable;
        mMongoDatabase = mongoDatabase;
    }

    public IEnumerator<T> GetEnumerator() => Queryable.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Type ElementType => Queryable.ElementType;
    public Expression Expression => Queryable.Expression;
    public IQueryProvider Provider => Queryable.Provider;

    public bool All(Expression<Func<T, bool>> predicate) => Queryable.All(predicate);
    public TAccumulate Aggregate<TAccumulate>(TAccumulate seed, Expression<Func<TAccumulate, T, TAccumulate>> func) => Queryable.Aggregate(seed, func);

    public IDbQueryable<T> Skip(int count) { Queryable = Queryable.Skip(count); return this; }
    public IDbQueryable<T> Take(int count) { Queryable = Queryable.Take(count); return this; }

    public IDbQueryable<TResult> Join<TInner, TResult>(Expression<Func<T, Guid>> keySelector,
        Expression<Func<T,TInner,TResult>> resultSelector) where TInner : IDbEntity =>
        new MongoQueryableProxy<TResult>(Queryable.Join(
            mMongoDatabase.GetCollection<TInner>(), 
            keySelector, 
            x => x.Id, 
            resultSelector), mMongoDatabase);

    public IDbQueryable<TResult> JoinGroup<TInner, TResult>(Expression<Func<T, Guid>> outerKeySelector,
        Expression<Func<TInner, Guid>> innerKeySelector,
        Expression<Func<T,IEnumerable<TInner>,TResult>> resultSelector) where TInner : IDbEntity =>
        new MongoQueryableProxy<TResult>(Queryable.GroupJoin(
            mMongoDatabase.GetCollection<TInner>(), outerKeySelector, innerKeySelector, resultSelector), mMongoDatabase);

    public IDbQueryable<T> Where(Expression<Func<T, bool>> predicate)  { Queryable = Queryable.Where(predicate); return this; }
    public IDbQueryable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector) => new MongoQueryableProxy<TResult>(Queryable.Select(selector), mMongoDatabase);
    public IDbQueryable<TResult> SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector) => new MongoQueryableProxy<TResult>(Queryable.SelectMany(selector), mMongoDatabase);
    public IDbOrderedQueryable<T> OrderBy<TResult>(Expression<Func<T, TResult>> selector) => new MongoOrderedQueryableProxy<T>(Queryable.OrderBy(selector), mMongoDatabase);
    public IDbOrderedQueryable<T> OrderByDescending<TResult>(Expression<Func<T, TResult>> selector) => new MongoOrderedQueryableProxy<T>(Queryable.OrderByDescending(selector), mMongoDatabase);
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => Queryable.AnyAsync(cancellationToken);
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Queryable.AnyAsync(predicate, cancellationToken);
    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Queryable.CountAsync(cancellationToken);
    public Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Queryable.CountAsync(predicate, cancellationToken);
    public Task<long> LongCountAsync(CancellationToken cancellationToken = default) => Queryable.LongCountAsync(cancellationToken);
    public Task<long> LongCountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Queryable.LongCountAsync(predicate, cancellationToken);
    public Task<T> FirstAsync(CancellationToken cancellationToken = default) => Queryable.FirstAsync(cancellationToken);
    public Task<T> FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Queryable.FirstAsync(predicate, cancellationToken);
    public Task<T> SingleAsync(CancellationToken cancellationToken = default) => Queryable.SingleAsync(cancellationToken);
    public Task<T> SingleAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Queryable.SingleAsync(predicate, cancellationToken);
    public Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default) => Queryable.FirstOrDefaultAsync(cancellationToken);
    public Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Queryable.FirstOrDefaultAsync(predicate, cancellationToken);
    public Task<T> SingleOrDefaultAsync(CancellationToken cancellationToken = default) => Queryable.SingleOrDefaultAsync(cancellationToken);
    public Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Queryable.SingleOrDefaultAsync(predicate, cancellationToken);
    public Task<T> MaxAsync(CancellationToken cancellationToken = default) => Queryable.MaxAsync(cancellationToken);
    public Task<TResult> MaxAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default) => Queryable.MaxAsync(selector, cancellationToken);
    public Task<T> MinAsync(CancellationToken cancellationToken = default) => Queryable.MinAsync(cancellationToken);
    public Task<TResult> MinAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default) => Queryable.MinAsync(selector, cancellationToken);
    public Task<List<T>> ToListAsync(CancellationToken cancellationToken = default) => Queryable.ToListAsync(cancellationToken);
        
}

internal class MongoOrderedQueryableProxy<T> : MongoQueryableProxy<T>, IDbOrderedQueryable<T>
{
    public IDbOrderedQueryable<T> ThenBy<TResult>(Expression<Func<T, TResult>> selector) { Queryable = ((IOrderedMongoQueryable<T>)Queryable).ThenBy(selector); return this; }
    public IDbOrderedQueryable<T> ThenByDescending<TResult>(Expression<Func<T, TResult>> selector) { Queryable = ((IOrderedMongoQueryable<T>)Queryable).ThenByDescending(selector); return this; }

    public MongoOrderedQueryableProxy(IOrderedMongoQueryable<T> queryable, IMongoDatabase mongoDatabase) : base(queryable, mongoDatabase)
    {
    }
}