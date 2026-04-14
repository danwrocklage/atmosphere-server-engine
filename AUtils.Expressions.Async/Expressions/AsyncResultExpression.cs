using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

/// <summary>
/// Represents return from asynchronous lambda function.
/// </summary>
/// <remarks>
/// This expression turns async state machine into final state.
/// </remarks>
public sealed class AsyncResultExpression : Expression
{
    private readonly TaskType mTaskType;

    internal AsyncResultExpression(Expression? result, TaskType taskType)
    {
        mTaskType = taskType;
        AsyncResult = result ?? Default(taskType.ResultType);
    }

    internal AsyncResultExpression(TaskType taskType)
        : this(null, taskType)
    {
    }

    /// <summary>
    /// Constructs non-void return from asynchronous lambda function.
    /// </summary>
    /// <param name="result">An expression representing result to be returned from asynchronous lambda function.</param>
    /// <param name="valueTask"><see langword="true"/>, to represent the result as <see cref="ValueTask"/> or <see cref="ValueTask{TResult}"/>.</param>
    public AsyncResultExpression(Expression result, bool valueTask)
    {
        AsyncResult = result;
        mTaskType = new TaskType(result.Type, valueTask);
    }

    /// <summary>
    /// Constructs void return from asynchronous lambda function.
    /// </summary>
    /// <param name="valueTask"><see langword="true"/>, to represent the result as <see cref="ValueTask"/>.</param>
    public AsyncResultExpression(bool valueTask)
        : this(Empty(), valueTask)
    {
    }

    /// <summary>
    /// An expression representing result to be returned from asynchronous lambda function.
    /// </summary>
    public Expression AsyncResult { get; }

    /// <summary>
    /// Type of this expression.
    /// </summary>
    /// <remarks>
    /// The type of this expression is <see cref="Task"/>, <see cref="Task{TResult}"/>, <see cref="ValueTask"/> or <see cref="ValueTask{TResult}"/>.
    /// </remarks>
    public override Type Type => mTaskType;

    /// <summary>
    /// Translates this expression into predefined set of expressions
    /// using Lowering technique.
    /// </summary>
    /// <returns>Translated expression.</returns>
    public override Expression Reduce()
    {
        Expression completedTask, failedTask;
        var caughtException = Variable(typeof(Exception));
        if (AsyncResult.Type == typeof(void))
        {
            completedTask = Block(AsyncResult, Default(typeof(CompletedTask)));
            
            var ctor = typeof(CompletedTask).GetConstructor(new [] {caughtException.Type});
            failedTask = New(ctor!, caughtException);
        }
        else
        {
            var completeCtor = typeof(CompletedTask<>).MakeGenericType(AsyncResult.Type)
                .GetConstructor(new[] {AsyncResult.Type});
            completedTask = New(completeCtor!, AsyncResult);
            
            var failedCtor = typeof(CompletedTask<>).MakeGenericType(AsyncResult.Type)
                .GetConstructor(new[] {typeof(Exception)});
            failedTask = New(failedCtor!, caughtException);
        }

        return AsyncResult is ConstantExpression or DefaultExpression ?
            Convert(completedTask,mTaskType) :
            Convert(TryCatch(completedTask, Catch(caughtException, failedTask)),mTaskType);
    }

    internal Expression Reduce(ParameterExpression stateMachine, LabelTarget endOfAsyncMethod)
    {
        // if state machine is non-void then use Result property
        var resultProperty = stateMachine.Type.GetProperty(nameof(AsyncStateMachine<ValueTuple, int>.Result));
        return resultProperty is null ?
            Block(AsyncResult, Call(stateMachine, nameof(AsyncStateMachine<ValueTuple>.Complete), Type.EmptyTypes), Return(endOfAsyncMethod)) :
            Block(Assign(Property(stateMachine, resultProperty), AsyncResult), Return(endOfAsyncMethod));
    }

    public override bool CanReduce => true;

    public override ExpressionType NodeType => ExpressionType.Extension;

    /// <summary>
    /// Visit children expressions.
    /// </summary>
    /// <param name="visitor">Expression visitor.</param>
    /// <returns>Potentially modified expression if one of children expressions is modified during visit.</returns>
    protected override AsyncResultExpression VisitChildren(ExpressionVisitor visitor)
    {
        var expression = visitor.Visit(AsyncResult);
        return ReferenceEquals(expression, AsyncResult) ? this : new(expression, mTaskType);
    }
}