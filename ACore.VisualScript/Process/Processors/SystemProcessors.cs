using System.Linq.Expressions;
using System.Reflection;
using ACore.Abstractions.Logging;

namespace ACore.VisualScript.Processors;

[NodeType("if")]
public class IfNodeProcessor : INodeProcessor
{
    public void Run(NodeContext context)
    {
        context.AddToStack(parameters => Expression
            .IfThenElse(context["Value"], parameters[0], parameters.Length > 1 ? parameters[1] : context.Return()));
    }
}
    
[NodeType("return")]
public class ReturnNodeProcessor : INodeProcessor
{
    public void Run(NodeContext context)
    {
        context.AddToStack(context.Return());
    }
}

[NodeType("log")]
public class LogNodeProcessor : INodeProcessor
{
    private static readonly MethodInfo sLogMethod = typeof(ILogger)
        .GetMethod(
            nameof(ILogger.Log),
            BindingFlags.Public | BindingFlags.Instance, 
            new[] { typeof(string), typeof(string), typeof(LogLevel) }) ?? throw new InvalidOperationException();
    
    public void Run(NodeContext context)
    {
        context.AddToStack(Expression.Call(
            Expression.Constant(null, typeof(ILogger)), 
            sLogMethod, 
            context["Category"], 
            context["Message"], 
            context["Level"]));
    }
}
    
[NodeType("random")]
public class RandomNodeProcessor : INodeProcessor
{
    private static readonly MethodInfo sRandomNextMethod = typeof(Random)
        .GetMethod(
            nameof(Random.Next),
            BindingFlags.Public | BindingFlags.Instance, 
            new[] { typeof(int), typeof(int) }) ?? throw new InvalidOperationException();
        
    public void Run(NodeContext context)
    {
        context["Value"] = Expression.Call(Expression.Constant(Random.Shared, typeof(Random)), 
            sRandomNextMethod,
            context["Min"], context["Max"]);
    }
}

[NodeType("var.getset")]
public class SetVarWithGetterNodeProcessor : INodeProcessor
{
    public void Run(NodeContext context)
    {
        var varName = GetVarNodeProcessor.GetVariableName(context["Name"]);
        context.AddToStack(Expression.Assign(context.GetVariable(varName), context["Value"]));
    }
}
    
[NodeType("var.set")]
public class SetVarNodeProcessor : INodeProcessor
{
    public void Run(NodeContext context)
    {
        var variable = context["Input"];
        var value = context["Value"];

        if (variable.Type != value.Type)
            throw new InvalidOperationException();
            
        context.AddToStack(Expression.Assign(context["Input"], context["Value"]));
    }
}
    
[NodeType("var.get")]
public class GetVarNodeProcessor : INodeProcessor
{
    public void Run(NodeContext context)
    {
        var nameString = GetVariableName(context["Name"]);

        context["Value"] = context.GetVariable(nameString);
    }

    internal static string GetVariableName(Expression expression)
    {
        if (expression is not ConstantExpression name || name.Type != typeof(string))
            throw new InvalidOperationException("'Name' for variable getter must be string constant");

        var nameString = name.Value as string ?? string.Empty;
        if (string.IsNullOrEmpty(nameString))
            throw new InvalidOperationException("'Name' for variable getter must be string constant");
            
        return nameString;
    }
}