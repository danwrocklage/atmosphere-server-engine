using System.Diagnostics;
using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

/// <summary>
/// Represents statement.
/// </summary>
internal sealed class ClosureExpression : Expression
{
    private readonly LambdaExpression mClosure;
    private readonly IReadOnlyDictionary<ParameterExpression, ParameterExpression> mApping;

    internal ClosureExpression(LambdaExpression closure, IReadOnlyDictionary<ParameterExpression, ParameterExpression> mapping)
    {
        Debug.Assert(closure is not null);
        Debug.Assert(mapping is not null);

        mClosure = closure;
        mApping = mapping;
    }

    public override bool CanReduce => mClosure.CanReduce;

    public override ExpressionType NodeType => ExpressionType.Extension;

    public override Type Type => mClosure.Type;

    public override Expression Reduce() => mClosure.Reduce();

    internal BlockExpression Reduce(IReadOnlyDictionary<ParameterExpression, MemberExpression?> stateMachineContext)
    {
        ICollection<Expression> body = new LinkedList<Expression>();
        foreach (var (k, v) in mApping)
        {
            if (stateMachineContext[k]?.Expression is MemberExpression inner)
                body.Add(Assign(v, inner));
        }

        body.Add(mClosure);
        return Block(mClosure.Type, mApping.Values, body);
    }
}