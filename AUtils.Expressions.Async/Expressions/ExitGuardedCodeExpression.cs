using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

internal sealed class ExitGuardedCodeExpression : TransitionExpression
{
    private readonly bool mSuspendException;

    internal ExitGuardedCodeExpression(uint parentState, bool suspendException)
        : base(parentState)
        => mSuspendException = suspendException;

    internal ExitGuardedCodeExpression(StatePlaceholderExpression placeholder, bool suspendException)
        : base(placeholder)
        => mSuspendException = suspendException;

    public override Type Type => typeof(void);

    public override Expression Reduce() => Empty();

    internal override Expression Reduce(ParameterExpression stateMachine)
        => Call(stateMachine, nameof(AsyncStateMachine<ValueTuple>.ExitGuardedCode), 
            new []{ typeof(uint), typeof(bool) }, StateId, Constant(mSuspendException));
}