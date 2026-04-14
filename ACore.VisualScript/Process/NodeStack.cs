using System.Linq.Expressions;

namespace ACore.VisualScript;

public class NodeStack
{
    private readonly Func<Expression[], Expression>? mResolver;
    private readonly List<object> mStackItems;

    internal NodeStack()
    {
        mResolver = null;
        mStackItems = new List<object>();
    }

    private NodeStack(Func<Expression[], Expression> resolver) : this()
    {
        mResolver = resolver;
    }
        
    internal Expression Resolve()
    {
        var output = mStackItems.Select(x => x switch
        {
            Expression xExp => xExp,
            NodeStack stack => stack.Resolve(),
            _ => throw new InvalidOperationException()
        }).ToArray();

        return mResolver?.Invoke(output) ?? Expression.Block(output);
    }

    public void Add(Expression stackItem)
    {
        if (stackItem == null) 
            throw new ArgumentNullException(nameof(stackItem));
            
        mStackItems.Add(stackItem);
    }

    public NodeStack Add(Func<Expression[], Expression> resolver)
    {
        if (resolver == null) 
            throw new ArgumentNullException(nameof(resolver));

        var newStack = new NodeStack(resolver);
        mStackItems.Add(newStack);
        return newStack;
    }
}