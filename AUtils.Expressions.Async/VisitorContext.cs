using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

internal sealed class VisitorContext : IDisposable
{
    private readonly Dictionary<LabelTarget, StatePlaceholderExpression> mPlaceholders = new();
    private readonly Dictionary<Expression, ExpressionAttributes?> mAttributeLinks = new();
    private readonly Stack<ExpressionAttributes> mAttributes;
    private readonly Stack<Statement> mStatements;
    private uint mStateId;
    private uint mPreviousStateId;

    internal VisitorContext(out LabelTarget asyncMethodEnd)
    {
        asyncMethodEnd = Expression.Label("end_async_method");
        mAttributes = new Stack<ExpressionAttributes>();
        mStatements = new Stack<Statement>();
        mPlaceholders.Add(asyncMethodEnd, new StatePlaceholderExpression(IAsyncStateMachine<ValueTuple>.FINAL_STATE));
        mStateId = mPreviousStateId = IAsyncStateMachine<ValueTuple>.FINAL_STATE;
    }

    internal Statement CurrentStatement => mStatements.Peek();
    
    internal ExpressionAttributes? GetAttributes(Expression node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        return mAttributeLinks.TryGetValue(node, out var attributes) ? attributes : default;
    }

    private void AddAttributes(Expression key, ExpressionAttributes attributes)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (mAttributeLinks.ContainsKey(key))
            mAttributeLinks[key] = attributes;
        else
            mAttributeLinks.Add(key, attributes);
    }

    internal KeyValuePair<uint, StateTransition> NewTransition(IDictionary<uint, StateTransition> table)
    {
        // if we are in finally or catch block then all exceptions must be redirected to the parent catch or finally block
        mStateId += 1;
        var transition = new StateTransition(Expression.Label("state_" + mStateId), fResolveFaultLabel());
        var pair = new KeyValuePair<uint, StateTransition>(mStateId, transition);
        table.Add(pair);
        return pair;

        LabelTarget? fResolveFaultLabel()
        {
            bool skipNextGuardedStatement = false;
            foreach (var statement in mStatements)
            {
                switch (statement)
                {
                    case GuardedStatement guarded:
                        if (!skipNextGuardedStatement)
                            return guarded.FaultLabel;
                        skipNextGuardedStatement = false;
                        break;
                    case FinallyStatement:
                        skipNextGuardedStatement = true;
                        break;
                }
            }

            return null;
        }
    }

    private TStatement? FindStatement<TStatement>()
        where TStatement : Statement
    {
        foreach (var statement in mStatements)
        {
            if (statement is TStatement result)
                return result;
        }

        return null;
    }

    internal bool IsInFinally => FindStatement<FinallyStatement>() is not null;

    internal bool HasAwait
    {
        get
        {
            foreach (var attr in mAttributes)
            {
                if (ReferenceEquals(GetAttributes(CurrentStatement), attr))
                    break;
                else if (attr.ContainsAwait)
                    return true;
            }

            return false;
        }
    }

    internal ParameterExpression? ExceptionHolder => FindStatement<CatchStatement>()?.ExceptionVar;

    private void ContainsAwait()
    {
        foreach (var attr in mAttributes)
        {
            if (ReferenceEquals(GetAttributes(CurrentStatement), attr))
                return;
            attr.ContainsAwait = true;
        }
    }

    private void AttachLabel(LabelTarget? target)
    {
        if (target is null) 
            return;
        
        GetAttributes(CurrentStatement)?.Labels.Add(target);
        mPlaceholders[target].StateId = mStateId;
    }

    internal TOutput Rewrite<TInput, TOutput, TAttributes>(TInput expression, Converter<TInput, TOutput> rewriter, Action<TAttributes>? initializer = null)
        where TInput : Expression
        where TOutput : Expression
        where TAttributes : ExpressionAttributes, new()
    {
        var attr = new TAttributes { StateId = mStateId };
        initializer?.Invoke(attr);
        AddAttributes(expression, attr);

        var isStatement = false;
        switch (expression)
        {
            case LabelExpression label:
                AttachLabel(label.Target);
                break;
            case GotoExpression @goto:
                if (!mPlaceholders.ContainsKey(@goto.Target))
                    mPlaceholders.TryAdd(@goto.Target, new StatePlaceholderExpression());
                break;
            case LoopExpression loop:
                AttachLabel(loop.ContinueLabel);
                AttachLabel(loop.BreakLabel);
                break;
            case Statement statement:
                mStatements.Push(statement);
                isStatement = true;
                break;
            case AwaitExpression _:
                attr.ContainsAwait = true;
                ContainsAwait();
                break;
        }

        mAttributes.Push(attr);
        var result = rewriter(expression);
        AddAttributes(result, mAttributes.Pop());
        if (isStatement)
        {
            mStatements.Pop();
            mPreviousStateId = attr.StateId;
        }

        return result;
    }

    internal TOutput Rewrite<TInput, TOutput>(TInput expression, Converter<TInput, TOutput> rewriter)
        where TInput : Expression
        where TOutput : Expression
        => Rewrite<TInput, TOutput, ExpressionAttributes>(expression, rewriter);

    internal Expression Rewrite(TryExpression expression, IDictionary<uint, StateTransition> transitionTable, Converter<TryCatchFinallyStatement, Expression> rewriter)
    {
        var previousStateId = mPreviousStateId;
        var statement = new TryCatchFinallyStatement(expression, transitionTable, previousStateId, ref mStateId);
        return Rewrite<TryCatchFinallyStatement, Expression, ExpressionAttributes>(statement, rewriter, attributes => attributes.StateId = previousStateId);
    }

    internal IReadOnlyCollection<Expression> CreateJumpPrologue(GotoExpression @goto, ExpressionVisitor visitor)
    {
        var result = new LinkedList<Expression>();

        // iterate through snapshot of statements because collection can be modified
        var statements = mStatements.ToArray();
        foreach (var lookup in statements)
        {
            if (GetAttributes(lookup)?.Labels.Contains(@goto.Target) ?? false)
                break;
            if (lookup is TryCatchFinallyStatement statement)
                result.AddLast(statement.InlineFinally(visitor, mPlaceholders[@goto.Target]));
        }

        Array.Clear(statements);
        return result;
    }

    public void Dispose()
    {
        mAttributes.Clear();
        mStatements.Clear();
    }
}