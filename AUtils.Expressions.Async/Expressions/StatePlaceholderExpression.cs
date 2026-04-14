using System.Diagnostics;
using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

internal sealed class StatePlaceholderExpression : Expression
{
    private uint? mStateId;

    internal StatePlaceholderExpression(uint stateId)
        => this.mStateId = stateId;

    public StatePlaceholderExpression()
        => mStateId = null;

    internal uint StateId
    {
        set => mStateId = value;
    }

    public override bool CanReduce => mStateId.HasValue;

    public override ExpressionType NodeType => ExpressionType.Extension;

    public override Type Type => typeof(uint);

    public override Expression Reduce()
    {
        Debug.Assert(mStateId.HasValue);
        return Constant(mStateId.Value);
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor) => this;
}