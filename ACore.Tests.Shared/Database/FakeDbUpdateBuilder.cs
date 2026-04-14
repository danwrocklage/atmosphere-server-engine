using System.Linq.Expressions;
using ACore.Abstractions.Database;

namespace ACore.Tests.Shared.Database;

public class FakeDbUpdateBuilder<T> : IDbUpdateBuilder<T> where T : IDbEntity
{
    private readonly ParameterExpression mXParam;
    private readonly IEnumerable<T> mEntities;
    private readonly List<Expression> mUpdaters = new();

    public FakeDbUpdateBuilder(IEnumerable<T> entities)
    {
        mEntities = entities;
        mXParam = Expression.Parameter(typeof(T), "x");
    }

    public IDbUpdateBuilder<T> Set<TField>(Expression<Func<T, TField>> expression, TField value)
    {
        if (expression.Body is not MemberExpression memberExpression)
            throw new ArgumentException("Only member accessing is supported", $"{nameof(expression)}");

        Expression valueExpression = Expression.Constant(value);
        if (memberExpression.Type != value.GetType())
            valueExpression = Expression.Convert(valueExpression, memberExpression.Type);

        mUpdaters.Add(Expression.Assign(Expression.MakeMemberAccess(mXParam, memberExpression.Member),
            valueExpression));
        return this;
    }

    public IDbUpdateBuilder<T> Inc<TField>(Expression<Func<T, TField>> expression, TField value)
        where TField : struct => this;

    public IDbUpdateBuilder<T> Push<TField>(Expression<Func<T, IEnumerable<TField>>> expression, TField value) => this;

    public Task<long> Apply(bool isUpsert = false, CancellationToken token = default)
    {
        if (mEntities == null || !mEntities.Any())
            return Task.FromResult<long>(default);

        var lambda = Expression
            .Lambda<Action<T>>(Expression.Block(Array.Empty<ParameterExpression>(), mUpdaters), mXParam)
            .Compile();

        foreach (var entity in mEntities)
            lambda(entity);

        return Task.FromResult<long>(default);
    }
}