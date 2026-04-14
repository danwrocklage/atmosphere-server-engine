using System.Linq.Expressions;
using System.Reflection;
using ACore.Abstractions;
using ACore.VisualScript.Models;
using AUtils.Expressions.Async;
using AUtils.IoC;

namespace ACore.VisualScript;

internal class NodeResolver
{
    #region Static reflection

    private static readonly Dictionary<string, ExpressionType> sExpressionTypes = Enum
        .GetValues<ExpressionType>()
        .ToDictionary(x => Enum.GetName(x)?.ToUpperInvariant() ?? throw new InvalidOperationException(), x => x);

    private static readonly Dictionary<string, Type> sProcessorsTypes = Types.All
        .Where(x => x.GetInterface(nameof(INodeProcessor)) == typeof(INodeProcessor) &&
                    !string.IsNullOrEmpty(x.GetCustomAttribute<NodeTypeAttribute>()?.Type))
        .ToDictionary(x => x.GetCustomAttribute<NodeTypeAttribute>()?.Type ?? string.Empty, x => x);
    
    private static readonly Dictionary<string, Type> sAsyncProcessorsTypes = Types.All
        .Where(x => x.GetInterface(nameof(IAsyncNodeProcessor)) == typeof(IAsyncNodeProcessor) &&
                    !string.IsNullOrEmpty(x.GetCustomAttribute<NodeTypeAttribute>()?.Type))
        .ToDictionary(x => x.GetCustomAttribute<NodeTypeAttribute>()?.Type ?? string.Empty, x => x);

    #endregion

    private readonly IContainer mContainer;
    private readonly NodeStack mRootStack;
    private readonly Dictionary<string, ParameterExpression> mVariables;
    private readonly Dictionary<string, ParameterExpression> mParameters;
    private bool mIsAsync;
    private IReadOnlyCollection<NodeUnit> mSourceUnits;

    public NodeResolver(IContainer container, IReadOnlyCollection<NodeUnit> sourceUnits)
    {
        mVariables = new Dictionary<string, ParameterExpression>();
        mParameters = new Dictionary<string, ParameterExpression>();
        mRootStack = new NodeStack();
        mContainer = container;
        mSourceUnits = sourceUnits;
    }

    public async Task<(LambdaExpression? Method, bool IsAsync)> Resolve()
    {
        SortSourceNodes();

        foreach (var unit in mSourceUnits)
            await ResolveUnit(unit);

        var resolved = mRootStack.Resolve();
        
        var returnWriter = new ReturnWriterVisitor(mIsAsync);
        resolved = returnWriter.Visit(resolved) ?? resolved;
        
        // Get expressions for block
        var expressions = resolved is BlockExpression blockExpression
            ? (IEnumerable<Expression>) blockExpression.Expressions
            : new[] {resolved};

        // If method is synchronous, add main return label at the end
        var returnLabel = returnWriter.ReturnTarget == null
            ? Array.Empty<Expression>()
            : new[] {Expression.Label(returnWriter.ReturnTarget)};
        
        var body = Expression.Block(mVariables.Values, expressions.Concat(returnLabel));
        return mIsAsync ? (AsyncExpression.Lambda(body, mParameters.Values.ToArray(),
            returnWriter.ReturnType == null ? typeof(Task) : typeof(Task<>).MakeGenericType(returnWriter.ReturnType),
            false, false), true) : (
                Expression.Lambda(body, false, mParameters.Values), false);
    }

    private void SortSourceNodes()
    {
        var startNodes = mSourceUnits
            .Where(x => x.FlowInputs == null || x.FlowInputs.Length == 0 && x.FlowOutputs.Count > 0)
            .ToArray();

        if (startNodes.Length > 1)
            throw new NodeCompileException("There are more than one start node", null);

        short id = 0;
        SetOrderToUnit(startNodes[0], ref id);

        mSourceUnits = mSourceUnits.OrderBy(x => x.Order).ToArray();
    }

    private void SetOrderToUnit(NodeUnit source, ref short current)
    {
        source.Order = current;
        current++;

        foreach (var nodeLink in source.DataInputs)
            SetOrderToUnit(nodeLink.Value.Unit, ref current);

        if (source.Description.IsFlow())
        {
            foreach (var nodeUnit in source.FlowOutputs.Values)
                SetOrderToUnit(nodeUnit, ref current);
        }
    }

    private async Task ResolveUnit(NodeUnit unit)
    {
        if (unit.Description.IsFlow() && unit.NodeStack == null)
            unit.NodeStack = unit.FlowInputs?.FirstOrDefault()?.NodeStack ?? mRootStack;

        var context = new NodeContext(unit, await GetUnitInputs(unit), mVariables);

        if (unit.Description.Type.StartsWith("system.unary-op", StringComparison.InvariantCultureIgnoreCase))
        {
            var expressionType = sExpressionTypes[unit.Description.Type[15..].ToUpperInvariant()];
            context["Value"] = Expression.MakeUnary(expressionType, context["Input"], null!);
        }
        else if (unit.Description.Type.StartsWith("system.binary-op", StringComparison.InvariantCultureIgnoreCase))
        {
            var expressionType = sExpressionTypes[unit.Description.Type[17..].ToUpperInvariant()];
            context["Value"] = Expression.MakeBinary(expressionType, context["Left"], context["Right"]);
        }
        else
            await RunProcessor(unit, context);

        mIsAsync = context.IsAsync || mIsAsync;
        unit.DataOutput = context.GetOutput();
        if (unit.Description.IsFlow())
            unit.NodeStack = context.CurrentStack ?? mRootStack;
    }

    private async Task RunProcessor(NodeUnit unit, NodeContext context)
    {
        var isAsync = false;
        if (!sProcessorsTypes.TryGetValue(unit.Description.Type, out var processorType))
        {
            if(!sAsyncProcessorsTypes.TryGetValue(unit.Description.Type, out processorType))
                throw new NodeCompileException($"Invalid node type '{unit.Description.Type}'", null);
            isAsync = true;
        }

        try
        {
            if (isAsync)
                await ((IAsyncNodeProcessor) mContainer.Resolve(processorType)).RunAsync(context);
            else
                ((INodeProcessor) mContainer.Resolve(processorType)).Run(context);
        }
        catch (Exception e)
        {
            throw new NodeCompileException(
                $"Can't resolve {processorType.FullName} or {nameof(INodeProcessor.Run)} was failed", unit, e);
        }
    }

    private async Task<Dictionary<string, Expression>> GetUnitInputs(NodeUnit nodeUnit)
    {
        var contextInputs = new Dictionary<string, Expression>();
        foreach (var input in nodeUnit.Description.Input)
        {
            if (input.IsFlow)
                continue;

            if (!nodeUnit.DataInputs.TryGetValue(input.Name, out var inputLink))
            {
                if (!nodeUnit.DataValues.TryGetValue(input.Name, out var data))
                    throw new NodeCompileException($"There is no connected node and default value for {input}",
                        nodeUnit);

                contextInputs.Add(input.Name, data);
                continue;
            }

            if (inputLink.Unit == null)
                throw new NodeCompileException($"There is no connected node for {input} (Invalid {nameof(NodeLink)})",
                    nodeUnit);

            if (inputLink.Unit.DataOutput == null)
                await ResolveUnit(inputLink.Unit);

            if (inputLink.Unit.DataOutput == null)
                throw new NodeCompileException(
                    $"Connected node {inputLink.Unit} doesn't have an output (Invalid Dictionary)", nodeUnit);

            if (!inputLink.Unit.DataOutput.TryGetValue(inputLink.ConnectionName, out var inputExpression))
                throw new NodeCompileException(
                    $"Can't find in connected node output with name {inputLink.ConnectionName}", nodeUnit);

            contextInputs.Add(input.Name, inputExpression);
        }

        return contextInputs;
    }
}