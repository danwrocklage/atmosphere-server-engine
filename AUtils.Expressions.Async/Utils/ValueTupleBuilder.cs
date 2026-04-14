using System.Collections;
using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

/// <summary>
/// Represents value tuple builder with arbitrary number of tuple
/// items.
/// </summary>
/// <seealso cref="ValueTuple"/>
/// <seealso href="https://docs.microsoft.com/en-us/dotnet/csharp/tuples">Tuples</seealso>
internal sealed class ValueTupleBuilder : IDisposable, IEnumerable<Type>
{
    private readonly IList<Type> mItems = new List<Type>(7); // no more than 7 items because max number of generic arguments of tuple type
    private ValueTupleBuilder? mRest;

    /// <summary>
    /// Number of elements in the tuple.
    /// </summary>
    public int Count => mItems.Count + (mRest?.Count ?? 0);

    /// <summary>
    /// Constructs value tuple.
    /// </summary>
    /// <returns>Value tuple.</returns>
    public Type Build() => Count switch
    {
        0 => typeof(ValueTuple),
        1 => typeof(ValueTuple<>).MakeGenericType(mItems[0]),
        2 => typeof(ValueTuple<,>).MakeGenericType(mItems[0], mItems[1]),
        3 => typeof(ValueTuple<,,>).MakeGenericType(mItems[0], mItems[1], mItems[2]),
        4 => typeof(ValueTuple<,,,>).MakeGenericType(mItems[0], mItems[1], mItems[2], mItems[3]),
        5 => typeof(ValueTuple<,,,,>).MakeGenericType(mItems[0], mItems[1], mItems[2], mItems[3], mItems[4]),
        6 => typeof(ValueTuple<,,,,,>).MakeGenericType(mItems[0], mItems[1], mItems[2], mItems[3], mItems[4], mItems[5]),
        7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(mItems[0], mItems[1], mItems[2], mItems[3], mItems[4], mItems[5], mItems[6]),
        _ => typeof(ValueTuple<,,,,,,,>).MakeGenericType(mItems[0], mItems[1], mItems[2], mItems[3], mItems[4], mItems[5], mItems[6], mRest!.Build()),
    };

    private void Build(Expression instance, Span<MemberExpression> output)
    {
        for (var i = 0; i < mItems.Count; i++)
            output[i] = Expression.Field(instance, "Item" + (i + 1));
        if (mRest is not null)
        {
            instance = Expression.Field(instance, "Rest");
            mRest.Build(instance, output.Slice(7));
        }
    }

    /// <summary>
    /// Constructs expression tree based on value tuple type.
    /// </summary>
    /// <typeparam name="TExpression">Type of expression tree.</typeparam>
    /// <param name="expressionFactory">A function accepting value tuple type and returning expression tree.</param>
    /// <param name="expression">Constructed expression.</param>
    /// <returns>Sorted array of value tuple type components.</returns>
    public MemberExpression[] Build<TExpression>(Func<Type, TExpression> expressionFactory, out TExpression expression)
        where TExpression : Expression
    {
        expression = expressionFactory(Build());
        var fieldAccessExpression = new MemberExpression[Count];
        Build(expression, fieldAccessExpression.AsSpan());
        return fieldAccessExpression;
    }

    /// <summary>
    /// Adds a new component into tuple.
    /// </summary>
    /// <param name="itemType">The type of the tuple component.</param>
    public void Add(Type itemType)
    {
        if (Count < 7)
            mItems.Add(itemType);
        else if (mRest is null)
            mRest = new ValueTupleBuilder() { itemType };
        else
            mRest.Add(itemType);
    }

    /// <summary>
    /// Adds a new component into tuple.
    /// </summary>
    /// <typeparam name="T">The type of the tuple component.</typeparam>
    public void Add<T>() => Add(typeof(T));

    /// <summary>
    /// Returns an enumerator over all tuple components.
    /// </summary>
    /// <returns>An enumerator over all tuple components.</returns>
    public IEnumerator<Type> GetEnumerator()
        => (mRest is null ? mItems : mItems.Concat(mRest)).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Releases all managed resources associated with this builder.
    /// </summary>
    public void Dispose()
    {
        mItems.Clear();
        mRest?.Dispose();
    }
}