using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

internal sealed class EnterGuardedCodeExpression : TransitionExpression
{
    internal EnterGuardedCodeExpression(uint stateId)
        : base(stateId)
    {
    }

    public override Type Type => typeof(void);

    public override Expression Reduce() => Empty();

    internal override Expression Reduce(ParameterExpression stateMachine)
        => Call(stateMachine, nameof(AsyncStateMachine<ValueTuple>.EnterGuardedCode), new []{typeof(uint)}, StateId);
}