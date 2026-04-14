using System.Linq.Expressions;

namespace ACore.Abstractions.Database;

public interface IDbOrderedQueryable<TSource> : IDbQueryable<TSource>
{
    IDbOrderedQueryable<TSource> ThenBy<TResult>(Expression<Func<TSource, TResult>> selector);
    IDbOrderedQueryable<TSource> ThenByDescending<TResult>(Expression<Func<TSource, TResult>> selector);
}