using System.Linq.Expressions;
using ACore.VisualScript.Models;
using AUtils.Expressions.Async;

namespace ACore.VisualScript;

public struct NodeContext
{
    private readonly NodeUnit mNodeUnit;
    private readonly string[] mRequiredOutputs;
    private readonly IDictionary<string, ParameterExpression> mVariables;
    private readonly Dictionary<string, Expression> mOutput;
    private readonly IReadOnlyDictionary<string, Expression> mInput;

    internal NodeContext(NodeUnit nodeUnit, IReadOnlyDictionary<string, Expression> input, 
        IDictionary<string, ParameterExpression> variables)
    {
        mNodeUnit = nodeUnit;
        mInput = input;
        mRequiredOutputs = nodeUnit.Description.Output
            .Where(x => !x.IsFlow)
            .Select(x => x.Name)
            .ToArray();
        mVariables = variables;
        CurrentStack = nodeUnit.Description.IsFlow() ? nodeUnit.NodeStack : null;
        mOutput = new Dictionary<string, Expression>();
    }

    internal NodeStack? CurrentStack { get; private set; }
    
    internal bool IsAsync { get; private set; }

    internal IReadOnlyDictionary<string, Expression> GetOutput()
    {
        var outputs = mOutput;
        if (mRequiredOutputs.Any(requiredOutput => !outputs.ContainsKey(requiredOutput)))
            throw new NodeCompileException("", mNodeUnit);

        return mOutput;
    }

    public ParameterExpression GetVariable(string name, Type type)
    {
        if (type == null) 
            throw new ArgumentNullException(nameof(type));
        
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (mVariables.TryGetValue(name, out var value)) 
            return value;
            
        mVariables.Add(name, Expression.Variable(type));
        return mVariables[name];
    }
    
    public ParameterExpression GetVariable(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (mVariables.TryGetValue(name, out var value)) 
            return value;

        throw new NodeCompileException("Undeclared variable", mNodeUnit);
    }

    public Expression Return() => new ReturnExpression(null);

    public Expression Return(Expression returnValue) => new ReturnExpression(returnValue);

    public Expression Await(Expression expression, bool configureAwait = false)
    {
        IsAsync = true;
        return new AwaitExpression(expression, configureAwait);
    }

    public string CurrentNode => mNodeUnit.Description.Type;

    public Expression this[string name]
    {
        get
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"'{name}' is null or empty");

            if (!mInput.TryGetValue(name, out var value) &&
                !mOutput.TryGetValue(name, out value))
                throw new NodeCompileException($"'{name}' is undefined in inputs and outputs", mNodeUnit);

            return value;
        }
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"'{name}' is null or empty");

            if (mInput.ContainsKey(name))
                throw new NodeCompileException($"'{name}' is defined in inputs", mNodeUnit);

            if (!mRequiredOutputs.Contains(name))
                throw new NodeCompileException($"'{name}' is not defined in outputs", mNodeUnit);

            if (!mOutput.ContainsKey(name))
                mOutput.Add(name, value);
            else
                mOutput[name] = value;
        }
    }

    public void AddToStack(Expression expression)
    {
        if (CurrentStack == null)
            throw new NodeCompileException("The node must be a flow node", mNodeUnit);

        CurrentStack.Add(expression);
    }

    public void AddToStack(Func<Expression[], Expression> deferredResolver)
    {
        if (deferredResolver == null)
            throw new ArgumentNullException(nameof(deferredResolver));

        if (CurrentStack == null)
            throw new NodeCompileException("The node must be a flow node", mNodeUnit);

        CurrentStack = CurrentStack.Add(deferredResolver);
    }
}