using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

internal abstract class StateMachineExpression : Expression
{
    public sealed override bool CanReduce => true;

    public sealed override ExpressionType NodeType => ExpressionType.Extension;

    internal abstract Expression Reduce(ParameterExpression stateMachine);

    protected override Expression VisitChildren(ExpressionVisitor visitor) => this;
}

internal abstract class TransitionExpression : StateMachineExpression
{
    private protected readonly Expression StateId;

    private protected TransitionExpression(uint state) => StateId = Constant(state);

    private protected TransitionExpression(StatePlaceholderExpression placeholder) => StateId = placeholder;

    private protected TransitionExpression(StateIdExpression state) => StateId = state;
}