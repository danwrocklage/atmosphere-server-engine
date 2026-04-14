using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

internal sealed class Inliner : ExpressionVisitor, IDisposable
{
    private readonly IDictionary<LabelTarget, LabelTarget> mLabelReplacement;

    private Inliner()
    {
        mLabelReplacement = new Dictionary<LabelTarget, LabelTarget>();
    }

    protected override LabelTarget? VisitLabelTarget(LabelTarget? node)
    {
        LabelTarget? targetCopy;
        if (node is null)
        {
            targetCopy = null;
        }
        else if (!mLabelReplacement.TryGetValue(node, out targetCopy))
        {
            targetCopy = Expression.Label(node.Type, node.Name);
            mLabelReplacement.Add(node, targetCopy);
        }

        return targetCopy;
    }

    void IDisposable.Dispose() => mLabelReplacement.Clear();

    internal static Expression Rewrite(Expression node)
    {
        using var rewriter = new Inliner();
        return rewriter.Visit(node);
    }
}