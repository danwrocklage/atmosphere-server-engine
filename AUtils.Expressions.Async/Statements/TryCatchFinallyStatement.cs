using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

internal sealed class TryCatchFinallyStatement : GuardedStatement
{
    private readonly uint mPreviousState;
    private readonly uint mRecoveryStateId;
    private readonly LabelTarget? mFinallyLabel;

    internal TryCatchFinallyStatement(TryExpression expression, IDictionary<uint, StateTransition> transitionTable, uint previousState, ref uint stateId)
        : base(expression, Label("fault_" + (++stateId)))
    {
        Prologue.AddFirst(new EnterGuardedCodeExpression(stateId));
        mPreviousState = previousState;
        transitionTable[stateId] = new StateTransition(null, FaultLabel);
        if (expression.Handlers.Count > 0)
        {
            mRecoveryStateId = ++stateId;
            mFinallyLabel = Label("finally_" + stateId);
            transitionTable[mRecoveryStateId] = new StateTransition(null, mFinallyLabel);
        }
    }

    internal new TryExpression Content => (TryExpression)base.Content;

    internal Expression InlineFinally(ExpressionVisitor visitor, StatePlaceholderExpression leavingState)
    {
        var finallyCode = Content.Finally;
        finallyCode = finallyCode is null ?
            new ExitGuardedCodeExpression(leavingState, false) :
            finallyCode.AddEpilogue(false, new []{new ExitGuardedCodeExpression(leavingState, true)});
        finallyCode = finallyCode.AddEpilogue(false, Epilogue);
        finallyCode = Inliner.Rewrite(finallyCode);
        return visitor.Visit(finallyCode);
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        // generate try block
        var tryBody = visitor.Visit(Wrap(Content.Body));
        tryBody = tryBody.AddPrologue(false, Prologue);
        if (mFinallyLabel is not null)
            tryBody = tryBody.AddEpilogue(false, new Expression[] {Goto(mFinallyLabel), Label(FaultLabel)});

        // generate exception handlers block
        var handlers = new LinkedList<Expression>();
        if (mFinallyLabel is not null)
        {
            handlers.AddLast(new ExitGuardedCodeExpression(mPreviousState, false));
            handlers.AddLast(new EnterGuardedCodeExpression(mRecoveryStateId));
            foreach (var handler in Content.Handlers)
                handlers.AddLast(visitor.Visit(new CatchStatement(handler, mFinallyLabel)));
        }

        // generate finally block
        Expression fault = new FinallyStatement(Content.Finally ?? Content.Fault ?? Empty(), mPreviousState, mFinallyLabel ?? FaultLabel);
        fault = visitor.Visit(fault);
        return tryBody.AddEpilogue(false, handlers).AddEpilogue(false, new []{fault});
    }
}