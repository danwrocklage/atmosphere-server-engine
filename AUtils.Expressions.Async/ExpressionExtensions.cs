using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

internal static class ExpressionExtensions
{
    internal static Expression AddPrologue(this Expression expression, bool inferType, IReadOnlyCollection<Expression> instructions)
    {
        if (instructions.Count == 0)
            return expression;
        if (expression is BlockExpression block)
            return Expression.Block(inferType ? block.Type : typeof(void), block.Variables, instructions.Concat(block.Expressions));
        return Expression.Block(inferType ? expression.Type : typeof(void), instructions.Append(expression));
    }

    internal static Expression AddEpilogue(this Expression expression, bool inferType, IReadOnlyCollection<Expression> instructions)
    {
        if (instructions.Count == 0)
            return expression;

        IEnumerable<Expression> result;
        IEnumerable<ParameterExpression> variables;
        if (expression is BlockExpression block)
        {
            variables = block.Variables;
            result = block.Expressions.Concat(instructions);
        }
        else
        {
            variables = Enumerable.Empty<ParameterExpression>();
            result = instructions.Prepend(expression);
        }

        return Expression.Block(inferType ? result.Last().Type : typeof(void), variables, result);
    }
}