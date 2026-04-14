using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

/// <summary>
/// Represents exception check inside of state machine.
/// </summary>
internal sealed class HasNoExceptionExpression : StateMachineExpression
{
    public override Type Type => typeof(bool);

    public override Expression Reduce() => Default(typeof(bool));

    internal override Expression Reduce(ParameterExpression stateMachine)
        => Property(stateMachine, nameof(AsyncStateMachine<ValueTuple>.HasNoException));
}