using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

internal sealed class StateIdExpression : StateMachineExpression
{
    public override Expression Reduce() => Constant(0U, typeof(uint));
    
    internal override Expression Reduce(ParameterExpression stateMachine)
        => Property(stateMachine, nameof(AsyncStateMachine<ValueTuple>.StateId));

    public override Type Type => typeof(uint);
}