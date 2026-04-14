using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

internal sealed class CatchStatement : GuardedStatement
{
    internal readonly ParameterExpression ExceptionVar;
    private readonly Expression mFilter;

    internal CatchStatement(CatchBlock handler, LabelTarget faultLabel)
        : base(handler.Body, faultLabel)
    {
        var recovery = new RecoverFromExceptionExpression(handler.Variable ?? Variable(handler.Test, "e"));
        mFilter = handler.Filter is null ? recovery : AndAlso(recovery, handler.Filter);
        ExceptionVar = recovery.Receiver;
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        var filter = visitor.Visit(this.mFilter);
        if (ExpressionAttributes.Get(filter) is { ContainsAwait: true })
            throw new NotSupportedException("");
        var handler = visitor.Visit(Content);
        handler = handler
            .AddPrologue(false, Prologue)
            .AddEpilogue(false, Epilogue)
            .AddEpilogue(false, new [] {Goto(FaultLabel)});
        return IfThen(filter, handler);
    }
}