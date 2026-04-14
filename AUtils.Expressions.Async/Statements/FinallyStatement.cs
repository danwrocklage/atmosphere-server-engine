using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

internal sealed class FinallyStatement : Statement
{
    internal FinallyStatement(Expression body, uint previousState, LabelTarget finallyLabel)
        : base(body)
    {
        Prologue.AddFirst(Label(finallyLabel));
        Prologue.AddLast(new ExitGuardedCodeExpression(previousState, true));
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
        => visitor.Visit(Content).AddPrologue(false, Prologue).AddEpilogue(false, Epilogue).AddEpilogue(false, new []{new RethrowExpression()});
}