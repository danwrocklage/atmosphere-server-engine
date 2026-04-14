using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AUtils.Expressions.Async;

/// <summary>
/// Represents suspension point in the execution of the lambda function until the awaited task completes.
/// </summary>
/// <seealso href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/await">Await expression</seealso>
public sealed class AwaitExpression : Expression
{
    private static readonly ConcurrentDictionary<ParameterExpression, bool> sIsAwaiterVar = new();

    /// <summary>
    /// Constructs <see langword="await"/> expression.
    /// </summary>
    /// <param name="expression">An expression providing asynchronous result in the form or <see cref="Task"/> or any other TAP pattern.</param>
    /// <param name="configureAwait"><see langword="true"/> to call <see cref="Task.ConfigureAwait(bool)"/> with <see langword="false"/> argument.</param>
    /// <exception cref="ArgumentException">Passed expression doesn't implement TAP pattern.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(Task))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(Task<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(ValueTask))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(ValueTask<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(TaskAwaiter))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(TaskAwaiter<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(ValueTaskAwaiter))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(ValueTaskAwaiter<>))]
    public AwaitExpression(Expression expression, bool configureAwait = false)
    {
        const BindingFlags cPublicInstanceMethod = BindingFlags.Public | BindingFlags.Instance;
        if (configureAwait)
        {
            MethodInfo? configureMethod = expression.Type.GetMethod(nameof(Task.ConfigureAwait), cPublicInstanceMethod, Type.DefaultBinder, new[] { typeof(bool) }, null);
            if (configureMethod is not null)
                expression = Call(expression, configureMethod, Constant(false, typeof(bool)));
        }

        // expression type must have type with GetAwaiter() method
        MethodInfo? getAwaiter = expression.Type.GetMethod(nameof(Task.GetAwaiter), cPublicInstanceMethod, Type.DefaultBinder, Type.EmptyTypes, null);
        GetAwaiter = Call(expression, getAwaiter ?? throw new ArgumentException());
        getAwaiter = GetAwaiter.Type.GetMethod(nameof(TaskAwaiter.GetResult), cPublicInstanceMethod, Type.DefaultBinder, Type.EmptyTypes, null);
        GetResultMethod = getAwaiter ?? throw new ArgumentException();
    }

    internal ParameterExpression NewAwaiterHolder()
    {
        var result = Variable(AwaiterType);
        sIsAwaiterVar.AddOrUpdate(result, _ => true, (_, _) => true);
        return result;
    }

    internal static bool IsAwaiterHolder([NotNullWhen(true)] ParameterExpression? variable)
        => variable != null && sIsAwaiterVar.TryGetValue(variable, out var value) && value;

    internal MethodCallExpression GetAwaiter { get; }

    internal Type AwaiterType => GetAwaiter.Type;

    internal MethodInfo GetResultMethod { get; }

    /// <summary>
    /// Gets result type of asynchronous operation.
    /// </summary>
    public override Type Type => GetResultMethod.ReturnType;

    public override bool CanReduce => true;

    public override ExpressionType NodeType => ExpressionType.Extension;

    /// <summary>
    /// Translates this expression into predefined set of expressions
    /// using Lowering technique.
    /// </summary>
    /// <returns>Translated expression.</returns>
    public override Expression Reduce() => Call(GetAwaiter, GetResultMethod);

    /// <summary>
    /// Visit children expressions.
    /// </summary>
    /// <param name="visitor">Expression visitor.</param>
    /// <returns>Potentially modified expression if one of children expressions is modified during visit.</returns>
    protected override AwaitExpression VisitChildren(ExpressionVisitor visitor)
    {
        Debug.Assert(GetAwaiter.Object is not null);
        var expression = visitor.Visit(GetAwaiter.Object);
        return ReferenceEquals(expression, GetAwaiter.Object) ? this : new(expression);
    }

    internal MethodCallExpression Reduce(ParameterExpression awaiterHolder, uint state, LabelTarget stateLabel, LabelTarget returnLabel, CodeInsertionPoint prologue)
    {
        prologue(Assign(awaiterHolder, GetAwaiter));
        prologue(Condition(new MoveNextExpression(awaiterHolder, state), Empty(), Return(returnLabel), typeof(void)));
        prologue(Label(stateLabel));
        return Call(awaiterHolder, GetResultMethod);
    }
}