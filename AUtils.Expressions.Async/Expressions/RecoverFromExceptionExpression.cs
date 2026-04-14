using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace AUtils.Expressions.Async;

internal sealed class RecoverFromExceptionExpression : StateMachineExpression
{
    internal readonly ParameterExpression Receiver;

    internal RecoverFromExceptionExpression(ParameterExpression receiver)
    {
        Receiver = receiver;
    }

    public override Expression Reduce() => Expression.Constant(true, typeof(bool));

    public override Type Type => typeof(bool);

    internal override Expression Reduce(ParameterExpression stateMachine)
    {
        MethodInfo? tryRecover = stateMachine.Type.GetMethod(nameof(AsyncStateMachine<ValueTuple>.TryRecover));
        Debug.Assert(tryRecover is not null);
        tryRecover = tryRecover.MakeGenericMethod(Receiver.Type);
        return Expression.Call(stateMachine, tryRecover, Receiver);
    }
}