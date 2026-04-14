using System.Linq.Expressions;

namespace ACore.VisualScript;

/// <summary>
/// Common expression for storing value for return
/// </summary>
public class ReturnExpression : Expression
{
    public ReturnExpression(Expression? returnValue)
    {
        ReturnValue = returnValue;
    }

    internal Expression? ReturnValue { get; }

    public override ExpressionType NodeType => ExpressionType.Extension;

    public override bool CanReduce => false;
}