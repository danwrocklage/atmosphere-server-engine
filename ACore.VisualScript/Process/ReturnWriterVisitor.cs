using System.Linq.Expressions;
using AUtils.Expressions.Async;

namespace ACore.VisualScript;

/// <summary>
/// Replace <see cref="ReturnExpression"/> for expression depends on method type (sync or async)
/// </summary>
internal class ReturnWriterVisitor : ExpressionVisitor
{
    private readonly bool mIsAsync;

    public ReturnWriterVisitor(bool isAsync)
    {
        mIsAsync = isAsync;
        ReturnType = null;
    }
    
    public LabelTarget? ReturnTarget { get; private set; }

    public Type? ReturnType { get; private set; }

    public override Expression? Visit(Expression? node)
    {
        if(node is not ReturnExpression returnExpression)
            return base.Visit(node);

        if (ReturnType != null && returnExpression.ReturnValue?.Type != ReturnType)
            throw new NodeCompileException("Different return types in script", null);

        ReturnType = returnExpression.ReturnValue?.Type ?? typeof(void);
        if(ReturnType != typeof(void) && returnExpression.ReturnValue == null)
            throw new NodeCompileException($"Return expression must be {ReturnType.FullName}", null);

        if(mIsAsync)
            return new AsyncResultExpression(returnExpression.ReturnValue ?? Expression.Default(ReturnType), false);

        ReturnTarget ??= Expression.Label(ReturnType, "main_return");

        return Expression.Return(ReturnTarget, returnExpression.ReturnValue, ReturnType);
    }
}