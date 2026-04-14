using System.Linq.Expressions;
using System.Reflection;

namespace AUtils.Expressions.Async;

internal sealed class MoveNextExpression : TransitionExpression
{
    private readonly uint mStateId;
    private readonly Expression mAwaiter;

    internal MoveNextExpression(Expression awaiter, uint stateId)
        : base(stateId)
    {
        mStateId = stateId;
        mAwaiter = awaiter;
    }

    public override Type Type => typeof(bool);

    public override Expression Reduce() => mAwaiter;

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        var newAwaiter = visitor.Visit(mAwaiter);
        return ReferenceEquals(mAwaiter, newAwaiter) ? this : new MoveNextExpression(newAwaiter, mStateId);
    }

    internal override Expression Reduce(ParameterExpression stateMachine)
    {
        const BindingFlags cPublicInstanceFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var genericParam = Type.MakeGenericMethodParameter(0).MakeByRefType();
        var moveNext = stateMachine.Type.GetMethod(nameof(AsyncStateMachine<ValueTuple>.MoveNext), 1, cPublicInstanceFlags, null, new[] { genericParam, typeof(uint) }, null)!.MakeGenericMethod(mAwaiter.Type);
        return Call(stateMachine, moveNext, mAwaiter, StateId);
    }
}