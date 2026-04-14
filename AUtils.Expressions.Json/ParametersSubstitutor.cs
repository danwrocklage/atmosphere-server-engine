using System.Linq.Expressions;

namespace AUtils.Expressions.Json;

internal class ParametersSubstitutor : ExpressionVisitor
{
    private readonly ICollection<string> mParameters;
    private readonly List<ParameterExpression> mExpressions;

    public ParametersSubstitutor(ICollection<string>? parameters)
    {
        mParameters = parameters ?? new List<string>();
        mExpressions = new List<ParameterExpression>();
    }
    
    public ParametersSubstitutor(List<ParameterExpression> expressions)
    {
        mParameters = new List<string>();
        mExpressions = expressions;
    }

    public IReadOnlyCollection<ParameterExpression> Parameters => mExpressions;

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (mParameters.Count > 0 && (string.IsNullOrEmpty(node.Name) || !mParameters.Contains(node.Name)))
            return base.VisitParameter(node);

        var existed = mExpressions
            .Find(x => x.Type == node.Type && x.Name == node.Name && x.NodeType == node.NodeType && x.IsByRef == node.IsByRef);

        if (existed != null)
            return existed;

        mExpressions.Add(node);
        return base.VisitParameter(node);
    }
}