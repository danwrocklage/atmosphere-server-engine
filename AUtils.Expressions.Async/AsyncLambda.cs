using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

public static class AsyncExpression
{
    private interface IExpressionBuilder
    {
        LambdaExpression Build(Expression body, ParameterExpression[] parameters, bool usePooling, bool tailCall);
    }
    
    private class ResultVisitor : ExpressionVisitor
    {
        public Type? ResultType { get; private set; }
        
        public override Expression? Visit(Expression? node)
        {
            if (node is AsyncResultExpression asyncNode)
                ResultType = asyncNode.Type;
            
            return base.Visit(node);
        }
    }
    
    private class ExpressionBuilder<TDelegate> : IExpressionBuilder where TDelegate : Delegate
    {
        public LambdaExpression Build(Expression body, ParameterExpression[] parameters, bool usePooling, bool tailCall)
        {
            var resultTypeVisitor = new ResultVisitor();
            resultTypeVisitor.Visit(body);
            var taskType = new TaskType(resultTypeVisitor.ResultType);
            
            if (body.Type != taskType)
                body = body.AddEpilogue(taskType.HasResult, new []{new AsyncResultExpression(taskType)});
        
            using var builder = new AsyncStateMachineBuilder<TDelegate>(parameters);
            return builder.Build(body, tailCall, usePooling);
        }
    }
    
    public static LambdaExpression? Lambda(Expression body, ParameterExpression[] parameters, Type returnType, bool usePooling, bool tailCall)
    {
        var typeArgs = new Type[parameters.Length + 1];
        Array.Copy(parameters.Select(x => x.Type).ToArray(), 0, typeArgs, 0, parameters.Length);
        typeArgs[^1] = returnType;
        
        var @delegate = Expression.GetDelegateType(typeArgs);
        var builder = (IExpressionBuilder?) Activator.CreateInstance(typeof(ExpressionBuilder<>).MakeGenericType(@delegate));
        return builder?.Build(body, parameters, usePooling, tailCall);
    }
}