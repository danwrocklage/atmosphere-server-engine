using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using BindingFlags = System.Reflection.BindingFlags;

namespace AUtils.Expressions.Async;

/// <summary>
/// Provides initial transformation of async method.
/// </summary>
/// <remarks>
/// Transformation steps:
/// 1. Identify all local variables
/// 2. Construct state holder type
/// 3. Replace all local variables with fields from state holder type.
/// </remarks>
internal sealed class AsyncStateMachineBuilder : ExpressionVisitor, IDisposable
{
    private static readonly ConcurrentDictionary<ParameterExpression, int> sVarsPositions = new();

    // small optimization - reuse variable for awaiters of the same type
    private sealed class VariableEqualityComparer : IEqualityComparer<ParameterExpression>
    {
        public bool Equals(ParameterExpression? x, ParameterExpression? y)
            => AwaitExpression.IsAwaiterHolder(x) && AwaitExpression.IsAwaiterHolder(y) ? x.Type == y.Type : object.Equals(x, y);

        public int GetHashCode(ParameterExpression variable)
            => AwaitExpression.IsAwaiterHolder(variable) ? variable.Type.GetHashCode() : variable.GetHashCode();
    }

    internal readonly TaskType Task;
    internal readonly Dictionary<ParameterExpression, MemberExpression?> Variables;
    private readonly VisitorContext mContext;
    internal ClosureAnalyzer? ClosureAnalyzer;

    // this label indicates end of async method when successful result should be returned
    internal readonly LabelTarget AsyncMethodEnd;

    // a table with labels in the beginning of async state machine
    private readonly SortedDictionary<uint, StateTransition> mStateSwitchTable;

    internal AsyncStateMachineBuilder(Type taskType, IReadOnlyList<ParameterExpression> parameters)
    {
        Task = new TaskType(taskType);
        Variables = new(new VariableEqualityComparer());
        for (var position = 0; position < parameters.Count; position++)
        {
            var parameter = parameters[position];
            MarkAsParameter(parameter, position);
            Variables.Add(parameter, null);
        }

        mContext = new VisitorContext(out AsyncMethodEnd);
        mStateSwitchTable = new SortedDictionary<uint, StateTransition>();
    }

    private static void MarkAsParameter(ParameterExpression parameter, int position)
    {
        sVarsPositions.AddOrUpdate(parameter, _ => position, (_, _) => position);
    }

    internal ParameterExpression[] Parameters =>
        Variables.Keys
            .Select(candidate =>
                new {candidate, position = sVarsPositions.TryGetValue(candidate, out var p) ? p : -1})
            .Where(t => t.position >= 0)
            .OrderBy(t => t.position)
            .Select(t => t.candidate)
            .ToArray();

    internal IEnumerable<ParameterExpression> Closures => Variables.Keys.Where(ClosureAnalyzer == null ?
                                                                               _ => false : ClosureAnalyzer.IsClosure);

    private ParameterExpression NewStateSlot(Type type)
        => NewStateSlot(() => Expression.Variable(type));

    private ParameterExpression NewStateSlot(Func<ParameterExpression> factory)
    {
        var slot = factory();
        Variables[slot] = null;
        return slot;
    }

    // async method cannot have block expression with type not equal to void
    protected override Expression VisitBlock(BlockExpression node)
    {
        if (node.Type == typeof(void))
        {
            Statement.Rewrite(ref node);
            foreach (var variable in node.Variables)
                Variables.Add(variable, null);
            node = node.Update(Enumerable.Empty<ParameterExpression>(), node.Expressions);
            return mContext.Rewrite(node, base.VisitBlock);
        }

        return VisitBlock(Expression.Block(typeof(void), node.Variables, node.Expressions));
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        if (node.Type == typeof(void))
        {
            node = node.Update(node.Test, Statement.Wrap(node.IfTrue), Statement.Wrap(node.IfFalse));
            return mContext.Rewrite(node, base.VisitConditional);
        }
        else if (node is { IfTrue: BlockExpression, IfFalse: BlockExpression })
        {
            throw new NotSupportedException();
        }
        else
        {
            /*
                x = a ? await b() : c();
                --transformed into--
                var temp;
                if(a)
                    temp = await b();
                else
                    temp = c();
                x = temp;
             */
            var prologue = mContext.CurrentStatement.PrologueCodeInserter();
            {
                var result = mContext.Rewrite(node, base.VisitConditional);
                if (result is ConditionalExpression conditional)
                    node = conditional;
                else
                    return result;
            }

            if (mContext.GetAttributes(node.IfTrue) is { ContainsAwait: true } || mContext.GetAttributes(node.IfFalse) is { ContainsAwait: true })
            {
                var tempVar = NewStateSlot(node.Type);
                prologue(Expression.Condition(node.Test, Expression.Assign(tempVar, node.IfTrue), Expression.Assign(tempVar, node.IfFalse), typeof(void)));
                return tempVar;
            }
            else
            {
                return node;
            }
        }
    }

    protected override Expression VisitLabel(LabelExpression node)
        => node.Type == typeof(void) ? mContext.Rewrite(node, labelExpression => labelExpression) : throw new NotSupportedException();

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        // inner lambda may have closures, we must handle this accordingly
        ClosureAnalyzer = new ClosureAnalyzer(Variables);
        var lambda = ClosureAnalyzer.Visit(node) as LambdaExpression;
        Debug.Assert(lambda is not null);

        return ClosureAnalyzer.Closures.Count > 0 ? new ClosureExpression(lambda, ClosureAnalyzer.Closures) : lambda;
    }

    protected override Expression VisitListInit(ListInitExpression node)
        => mContext.Rewrite(node, base.VisitListInit);

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        => mContext.Rewrite(node, base.VisitTypeBinary);

    protected override Expression VisitSwitch(SwitchExpression node)
    {
        if (node.Type != typeof(void)) 
            throw new NotSupportedException();
        
        Statement.Rewrite(ref node);
        return mContext.Rewrite(node, base.VisitSwitch);
    }

    protected override Expression VisitGoto(GotoExpression node)
    {
        node = mContext.Rewrite(node, gotoExpression => gotoExpression);
        return node.AddPrologue(false, mContext.CreateJumpPrologue(node, this));
    }

    protected override Expression VisitDebugInfo(DebugInfoExpression node)
        => mContext.Rewrite(node, base.VisitDebugInfo);

    protected override Expression VisitDefault(DefaultExpression node)
        => mContext.Rewrite(node, base.VisitDefault);

    protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        => mContext.Rewrite(node, base.VisitRuntimeVariables);

    protected override SwitchCase VisitSwitchCase(SwitchCase node)
    {
        Converter<Expression, Expression> visitor = Visit;
        var testValues = Visit(node.TestValues, tst => mContext.Rewrite(tst, visitor));
        var body = mContext.Rewrite(node.Body, visitor);
        return node.Update(testValues, body);
    }

    protected override ElementInit VisitElementInit(ElementInit node)
    {
        var arguments = Visit(node.Arguments, arg => mContext.Rewrite(arg, Visit));
        return node.Update(arguments);
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        var expression = mContext.Rewrite(node.Expression, new Converter<Expression, Expression>(Visit));
        return ReferenceEquals(expression, node.Expression) ? node : node.Update(expression);
    }

    protected override CatchBlock VisitCatchBlock(CatchBlock node)
        => throw new NotSupportedException();

    // try-catch will be completely replaced with flat code and set of switch-case-goto statements
    protected override Expression VisitTry(TryExpression node)
        => mContext.Rewrite(node, mStateSwitchTable, base.VisitExtension);

    private Expression VisitAwait(AwaitExpression node)
    {
        var prologue = mContext.CurrentStatement.PrologueCodeInserter();
        node = (AwaitExpression)base.VisitExtension(node);

        // allocate slot for awaiter
        var awaiterSlot = NewStateSlot(node.NewAwaiterHolder);

        // generate new state and label for it
        var (stateId, transition) = mContext.NewTransition(mStateSwitchTable);

        // convert await expression into TAwaiter.GetResult() expression
        return node.Reduce(awaiterSlot, stateId, transition.Successful ?? throw new InvalidOperationException(), AsyncMethodEnd, prologue);
    }

    private Expression VisitAsyncResult(AsyncResultExpression expr)
    {
        if (mContext.IsInFinally)
            throw new InvalidOperationException();

        // attach all available finalization code
        var prologue = mContext.CurrentStatement.PrologueCodeInserter();
        expr = (AsyncResultExpression)base.VisitExtension(expr);

        foreach (var finalization in mContext.CreateJumpPrologue(Expression.Goto(AsyncMethodEnd), this))
            prologue(finalization);
        return expr;
    }

    protected override Expression VisitExtension(Expression node)
    {
        switch (node)
        {
            case StatePlaceholderExpression placeholder:
                return placeholder;
            case AsyncResultExpression result:
                return VisitAsyncResult(result);
            case AwaitExpression @await:
                return mContext.Rewrite(@await, VisitAwait);
            case RecoverFromExceptionExpression recovery:
                Variables.Add(recovery.Receiver, null);
                return recovery;
            case StateMachineExpression sme:
                return sme;
            default:
                return mContext.Rewrite(node, base.VisitExtension);
        }
    }

    private static bool IsAssignment(BinaryExpression binary) => binary.NodeType is ExpressionType.Assign or
        ExpressionType.AddAssign or
        ExpressionType.AddAssignChecked or
        ExpressionType.SubtractAssign or
        ExpressionType.SubtractAssignChecked or
        ExpressionType.OrAssign or
        ExpressionType.AndAssign or
        ExpressionType.ExclusiveOrAssign or
        ExpressionType.DivideAssign or
        ExpressionType.LeftShiftAssign or
        ExpressionType.RightShiftAssign or
        ExpressionType.MultiplyAssign or
        ExpressionType.MultiplyAssignChecked or
        ExpressionType.ModuloAssign or
        ExpressionType.PostDecrementAssign or
        ExpressionType.PreDecrementAssign or
        ExpressionType.PostIncrementAssign or
        ExpressionType.PreIncrementAssign or
        ExpressionType.PowerAssign;

    private Expression RewriteBinary(BinaryExpression node)
    {
        var codeInsertionPoint = mContext.CurrentStatement.PrologueCodeInserter();
        var newNode = base.VisitBinary(node);
        if (newNode is BinaryExpression binary)
            node = binary;
        else
            return newNode;

        // do not place left operand at statement level because it has no side effects
        if (node.Left is ParameterExpression || node.Left is ConstantExpression || IsAssignment(node))
            return node;
        var leftIsAsync = mContext.GetAttributes(node.Left) is { ContainsAwait: true };
        var rightIsAsync = mContext.GetAttributes(node.Right) is { ContainsAwait: true };

        // left operand should be computed before right, so bump it before await expression
        if (rightIsAsync && !leftIsAsync)
        {
            /*
                Method() + await a;
                --transformed into--
                state.field = Method();
                state.awaiter = a.GetAwaiter();
                MoveNext(state.awaiter, newState);
                return;
                newState: state.field + state.awaiter.GetResult();
             */
            var leftTemp = NewStateSlot(node.Left.Type);
            codeInsertionPoint(Expression.Assign(leftTemp, node.Left));
            node = node.Update(leftTemp, node.Conversion, node.Right);
        }

        return node;
    }

    protected override Expression VisitBinary(BinaryExpression node)
        => mContext.Rewrite(node, RewriteBinary);

    protected override Expression VisitParameter(ParameterExpression node)
        => mContext.Rewrite(node, base.VisitParameter);

    protected override Expression VisitConstant(ConstantExpression node)
        => mContext.Rewrite(node, base.VisitConstant);

    private Expression RewriteCallable<TException>(TException node, Expression[] arguments, Converter<TException, Expression> visitor, Func<TException, Expression[], TException> updater)
        where TException : Expression
    {
        var newNode = visitor(node);
        if (newNode is TException typedExpr)
            node = typedExpr;
        else
            return newNode;

        var hasAwait = false;
        var codeInsertionPoint = mContext.CurrentStatement.PrologueCodeInserter();
        for (var i = arguments.LongLength - 1L; i >= 0L; i--)
        {
            ref Expression arg = ref arguments[i];
            hasAwait |= mContext.GetAttributes(arg) is { ContainsAwait: true };
            if (hasAwait)
            {
                var tempVar = NewStateSlot(arg.Type);
                codeInsertionPoint(Expression.Assign(tempVar, arg));
                arg = tempVar;
            }
        }

        return updater(node, arguments);
    }

    private static MethodCallExpression UpdateArguments(MethodCallExpression node, IReadOnlyCollection<Expression> arguments)
        => node.Update(node.Object!, arguments);

    protected override Expression VisitMethodCall(MethodCallExpression node)
        => mContext.Rewrite(node, n => RewriteCallable(n, n.Arguments.ToArray(), base.VisitMethodCall, UpdateArguments));

    private static InvocationExpression UpdateArguments(InvocationExpression node, IReadOnlyCollection<Expression> arguments)
        => node.Update(node.Expression, arguments);

    protected override Expression VisitInvocation(InvocationExpression node)
        => mContext.Rewrite(node, n => RewriteCallable(n, n.Arguments.ToArray(), base.VisitInvocation, UpdateArguments));

    private static IndexExpression UpdateArguments(IndexExpression node, IReadOnlyCollection<Expression> arguments)
        => node.Update(node.Object!, arguments);

    protected override Expression VisitIndex(IndexExpression node)
        => mContext.Rewrite(node, n => RewriteCallable(n, n.Arguments.ToArray(), base.VisitIndex, UpdateArguments));

    private static NewExpression UpdateArguments(NewExpression node, IReadOnlyCollection<Expression> arguments)
        => node.Update(arguments);

    protected override Expression VisitNew(NewExpression node)
        => mContext.Rewrite(node, n => RewriteCallable(n, n.Arguments.ToArray(), base.VisitNew, UpdateArguments));

    private static NewArrayExpression UpdateArguments(NewArrayExpression node, IReadOnlyCollection<Expression> arguments)
        => node.Update(arguments);

    protected override Expression VisitNewArray(NewArrayExpression node)
        => mContext.Rewrite(node, n => RewriteCallable(n, n.Expressions.ToArray(), base.VisitNewArray, UpdateArguments));

    protected override Expression VisitLoop(LoopExpression node)
    {
        if (node.Type != typeof(void))
            throw new NotSupportedException();

        Statement.Rewrite(ref node);
        return mContext.Rewrite(node, base.VisitLoop);
    }

    protected override Expression VisitDynamic(DynamicExpression node)
        => mContext.Rewrite(node, base.VisitDynamic);

    protected override Expression VisitMember(MemberExpression node)
        => mContext.Rewrite(node, base.VisitMember);

    protected override Expression VisitMemberInit(MemberInitExpression node)
        => mContext.Rewrite(node, base.VisitMemberInit);

    private Expression Rethrow(UnaryExpression node)
    {
        var holder = mContext.ExceptionHolder;
        return holder is null ? new RethrowExpression() : RethrowExpression.Dispatch(holder);
    }

    protected override Expression VisitUnary(UnaryExpression node) => node.NodeType switch
    {
        ExpressionType.Throw when node.Operand is null => mContext.Rewrite(node, Rethrow),
        _ => mContext.Rewrite(node, base.VisitUnary)
    };

    private SwitchExpression MakeSwitch()
    {
        ICollection<SwitchCase> cases = new LinkedList<SwitchCase>();
        foreach (var (state, label) in mStateSwitchTable)
            cases.Add(Expression.SwitchCase(label.MakeGoto(), Expression.Constant(state, typeof(uint))));
        return Expression.Switch(new StateIdExpression(), Expression.Empty(), null, cases);
    }

    private Expression Rewrite(Statement body)
        => Visit(body).Reduce().AddPrologue(false, new []{MakeSwitch()}).AddEpilogue(false, new []{Expression.Label(AsyncMethodEnd)});

    internal Expression Rewrite(Expression body)
        => Rewrite(body is BlockExpression block ?
            new Statement(Expression.Block(typeof(void), block.Variables, block.Expressions)) :
            new Statement(body));

    public void Dispose()
    {
        Variables.Clear();
        mStateSwitchTable.Clear();
        mContext.Dispose();
    }
}

internal sealed class AsyncStateMachineBuilder<TDelegate> : ExpressionVisitor, IDisposable
    where TDelegate : Delegate
{
    private readonly AsyncStateMachineBuilder mMethodBuilder;
    private ParameterExpression? mStateMachine;

    internal AsyncStateMachineBuilder(IReadOnlyList<ParameterExpression> parameters)
    {
        var delegateType = typeof(TDelegate);
        var invokeMethod = delegateType.IsSealed ?
            delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!
            : throw new InvalidOperationException();
        mMethodBuilder = new AsyncStateMachineBuilder(invokeMethod.ReturnType, parameters);
    }

    private static Type BuildTransitionDelegate(Type stateMachineType)
        => typeof(Transition<,>)
            .MakeGenericType(stateMachineType.GetGenericArguments(typeof(IAsyncStateMachine<>))[0], stateMachineType);

    private static LambdaExpression BuildStateMachine(Expression body, ParameterExpression stateMachine, bool tailCall)
        => Expression.Lambda(BuildTransitionDelegate(stateMachine.Type), body, tailCall, stateMachine);

    private static MemberExpression GetStateField(ParameterExpression stateMachine)
        => Expression.Field(stateMachine,nameof(AsyncStateMachine<int>.State));

    private Expression<TDelegate> Build(LambdaExpression stateMachineMethod)
    {
        Debug.Assert(mStateMachine is not null);
        var stateVariable = Expression.Variable(GetStateField(mStateMachine).Type);
        var parameters = mMethodBuilder.Parameters;
        ICollection<Expression> newBody = new LinkedList<Expression>();

        // initialize closure containers
        foreach (var localVar in mMethodBuilder.Closures)
        {
            if (mMethodBuilder.Variables[localVar]?.Expression is MemberExpression inner)
            {
                inner = inner.Update(stateVariable);
                newBody.Add(Expression.Assign(inner, Expression.New(inner.Type)));
            }
        }

        // save all parameters into fields
        foreach (var parameter in parameters)
        {
            var parameterHolder = mMethodBuilder.Variables[parameter];
            Debug.Assert(parameterHolder is not null);

            // detect closure
            if (mMethodBuilder.ClosureAnalyzer?.IsClosure(parameter) == true && parameterHolder.Expression is MemberExpression inner)
            {
                inner = inner.Update(stateVariable);
                parameterHolder = parameterHolder.Update(inner);
            }
            else
            {
                parameterHolder = parameterHolder.Update(stateVariable);
            }

            newBody.Add(Expression.Assign(parameterHolder, parameter));
        }

        var startMethod = mStateMachine.Type.GetMethod(nameof(AsyncStateMachine<ValueTuple>.Start));
        Debug.Assert(startMethod is not null);
        newBody.Add(mMethodBuilder.Task.AdjustTaskType(Expression.Call(startMethod, stateMachineMethod, stateVariable)));
        return Expression.Lambda<TDelegate>(Expression.Block(new[] { stateVariable }, newBody), true, parameters);
    }

    private sealed class StateMachineBuilder
    {
        private readonly bool mUsePooling;
        private readonly Type mReturnType;
        internal ParameterExpression? StateMachine;

        internal StateMachineBuilder(Type returnType, bool usePooling)
        {
            mReturnType = returnType;
            mUsePooling = usePooling;
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AsyncStateMachine<>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AsyncStateMachine<,>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PoolingAsyncStateMachine<>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PoolingAsyncStateMachine<,>))]
        internal MemberExpression Build(Type stateType)
        {
            Type stateMachineType;
            if (mReturnType == typeof(void))
            {
                stateMachineType = mUsePooling ? typeof(AsyncStateMachine<>) : typeof(PoolingAsyncStateMachine<>);
                stateMachineType = stateMachineType.MakeGenericType(stateType);
            }
            else
            {
                stateMachineType = mUsePooling ? typeof(AsyncStateMachine<,>) : typeof(PoolingAsyncStateMachine<,>);
                stateMachineType = stateMachineType.MakeGenericType(stateType, mReturnType);
            }

            stateMachineType = stateMachineType.MakeByRefType();
            return GetStateField(StateMachine = Expression.Parameter(stateMachineType));
        }
    }

    private MemberExpression[] CreateStateHolderType(Type returnType, bool usePooling, IReadOnlyList<ParameterExpression> variables, out ParameterExpression stateMachine)
    {
        var sm = new StateMachineBuilder(returnType, usePooling);
        MemberExpression[] slots;
        using (var builder = new ValueTupleBuilder())
        {
            foreach (var v in variables)
            {
                var type = mMethodBuilder.ClosureAnalyzer?.IsClosure(v) == true
                    ? typeof(StrongBox<>).MakeGenericType(v.Type)
                    : v.Type;
                builder.Add(type);
            }

            slots = builder.Build(sm.Build, out _);
        }

        Debug.Assert(sm.StateMachine is not null);
        stateMachine = sm.StateMachine;
        return slots;
    }

    private ParameterExpression CreateStateHolderType(Type returnType, bool usePooling, IDictionary<ParameterExpression, MemberExpression?> variables)
    {
        var vars = variables.Keys.ToArray();
        var slots = CreateStateHolderType(returnType, usePooling, vars, out var stateMachine);
        for (var i = 0L; i < slots.LongLength; i++)
        {
            var v = vars[i];
            var s = slots[i];
            variables[v] = mMethodBuilder.ClosureAnalyzer?.IsClosure(v) == true ? Expression.Field(s, nameof(StrongBox<int>.Value)) : s;
        }

        return stateMachine;
    }

    // replace local variables with appropriate state fields
    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (mMethodBuilder.Variables.TryGetValue(node, out var stateSlot))
        {
            Debug.Assert(stateSlot is not null);
            return stateSlot;
        }

        return node;
    }

    protected override Expression VisitExtension(Expression node)
    {
        Debug.Assert(mStateMachine is not null);
        return node switch
        {
            StatePlaceholderExpression placeholder => placeholder.Reduce(),
            AsyncResultExpression result => Visit(result.Reduce(mStateMachine, mMethodBuilder.AsyncMethodEnd)),
            StateMachineExpression sme => Visit(sme.Reduce(mStateMachine)),
            Statement statement => Visit(statement.Reduce()),
            ClosureExpression closure => closure.Reduce(mMethodBuilder.Variables),
            _ => base.VisitExtension(node),
        };
    }

    internal Expression<TDelegate> Build(Expression body, bool tailCall, bool usePooling)
    {
        body = mMethodBuilder.Rewrite(body) ?? Expression.Empty();

        // build state machine type
        mStateMachine = CreateStateHolderType(mMethodBuilder.Task.ResultType, usePooling, mMethodBuilder.Variables);

        // replace all special expressions
        body = Visit(body);

        // now we have state machine method, wrap it into lambda
        return Build(BuildStateMachine(body, mStateMachine, tailCall));
    }

    public void Dispose() => mMethodBuilder.Dispose();
}