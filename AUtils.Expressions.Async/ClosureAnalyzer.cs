using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace AUtils.Expressions.Async;

internal sealed class ClosureAnalyzer : ExpressionVisitor
{
    private readonly Dictionary<ParameterExpression, bool> mClosureVars = new();

    private readonly ICollection<ParameterExpression> mLocals;
    internal readonly Dictionary<ParameterExpression, ParameterExpression> Closures;

    internal ClosureAnalyzer(Dictionary<ParameterExpression, MemberExpression?> variables)
    {
        mLocals = variables.Keys;
        Closures = new(variables.Count, variables.Comparer);
    }

    [return: NotNullIfNotNull(nameof(node))]
    public override Expression? Visit(Expression? node)
    {
        if (node is ParameterExpression p && mLocals.Contains(p))
        {
            // replace local with closure variable
            var closure = Expression.Variable(typeof(StrongBox<>).MakeGenericType(p.Type));
            if (mClosureVars.ContainsKey(p))
                mClosureVars[p] = true;
            else
                mClosureVars.Add(p, true);
            Closures.Add(p, closure);
            return Expression.Field(closure, nameof(StrongBox<int>.Value));
        }

        return base.Visit(node);
    }

    internal bool IsClosure(ParameterExpression p)
        => mClosureVars.TryGetValue(p, out var result) | result;
}