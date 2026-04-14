using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

internal abstract class GuardedStatement : Statement
{
    internal readonly LabelTarget FaultLabel;

    private protected GuardedStatement(Expression expression, LabelTarget faultLabel)
        : base(expression)
    {
        FaultLabel = faultLabel;
    }
}