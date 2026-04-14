using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

/// <summary>
/// Represents compiler-generated attributes associated with every expression.
/// </summary>
internal class ExpressionAttributes
{
    private static readonly ConcurrentDictionary<Expression, ExpressionAttributes?> sAttributes = new();

    /// <summary>
    /// A set of labels owner by expression.
    /// </summary>
    internal readonly ISet<LabelTarget> Labels = new HashSet<LabelTarget>();

    /// <summary>
    /// Indicates that expression contains await expression.
    /// </summary>
    internal bool ContainsAwait;

    /// <summary>
    /// Represents state of the expression.
    /// </summary>
    internal uint StateId;
    
    internal static ExpressionAttributes? Get(Expression node) => 
        node != null && sAttributes.TryGetValue(node, out var attributes) ? attributes : default;
}