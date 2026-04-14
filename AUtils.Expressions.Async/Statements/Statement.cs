using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace AUtils.Expressions.Async;

internal delegate void CodeInsertionPoint(Expression expr);

/// <summary>
/// Represents statement.
/// </summary>
internal class Statement : Expression
{
    private sealed class CodeInsertionPoint
    {
        private object mNodeOrList;

        internal CodeInsertionPoint(LinkedList<Expression> list) => mNodeOrList = list;

        internal CodeInsertionPoint(LinkedListNode<Expression> node) => mNodeOrList = node;

        internal void Insert(Expression expr)
        {
            switch (mNodeOrList)
            {
                case LinkedList<Expression> list:
                    mNodeOrList = list.AddLast(expr);
                    break;
                case LinkedListNode<Expression> node:
                    Debug.Assert(node.List is not null);
                    mNodeOrList = node.List.AddAfter(node, expr);
                    break;
            }
        }
    }

    private protected readonly LinkedList<Expression> Prologue;
    private protected readonly LinkedList<Expression> Epilogue;
    internal readonly Expression Content;

    internal Statement(Expression expression)
        : this(expression, Enumerable.Empty<Expression>(), Enumerable.Empty<Expression>())
    {
    }

    private Statement(Expression expression, IEnumerable<Expression> prologue, IEnumerable<Expression> epilogue)
    {
        Content = expression ?? Empty();
        if (expression is Statement stmt)
        {
            InsertIntoHead(prologue, Prologue = stmt.Prologue);
            InsertIntoHead(epilogue, Epilogue = stmt.Epilogue);
        }
        else
        {
            Prologue = new LinkedList<Expression>(prologue);
            Epilogue = new LinkedList<Expression>(epilogue);
        }
    }

    private static void InsertIntoHead(IEnumerable<Expression> source, LinkedList<Expression> destination)
    {
        if (destination.First is null)
        {
            foreach(var node in source)
                destination.AddLast(node);
        }
        else
        {
            var first = destination.First;
            foreach (var expr in source)
                destination.AddBefore(first, expr);
        }
    }

    [return: NotNullIfNotNull(nameof(expr))]
    internal static Expression? Wrap(Expression? expr)
    {
        switch (expr)
        {
            case null:
                return null;
            case TryExpression seh:
                return seh;
            case BlockExpression block:
                Rewrite(ref block);
                return block;
            case LoopExpression loop:
                Rewrite(ref loop);
                return loop;
            case SwitchExpression sw:
                Rewrite(ref sw);
                return sw;
            case Statement stmt:
                return stmt;
            default:
                return new Statement(expr);
        }
    }

    internal static void Rewrite(ref LoopExpression loop)
        => loop = loop.Update(loop.BreakLabel, loop.ContinueLabel, Wrap(loop.Body));

    internal static void Rewrite(ref BlockExpression block)
        => block = block.Update(block.Variables, block.Expressions.Select(Wrap)!);

    internal static void Rewrite(ref SwitchExpression @switch)
        => @switch = @switch.Update(@switch.SwitchValue, @switch.Cases.Select(c => c.Update(c.TestValues, Wrap(c.Body))), Wrap(@switch.DefaultBody));

    private static CodeInsertionPoint CaptureRewritePoint(LinkedList<Expression> codeBlock)
    {
        if (codeBlock.First is null)
            return new CodeInsertionPoint(codeBlock);

        Debug.Assert(codeBlock.Last is not null);
        return new CodeInsertionPoint(codeBlock.Last);
    }

    internal Async.CodeInsertionPoint PrologueCodeInserter() => CaptureRewritePoint(Prologue).Insert;

    internal Async.CodeInsertionPoint EpilogueCodeInserter() => CaptureRewritePoint(Epilogue).Insert;

    public sealed override Type Type => Content.Type;

    public sealed override ExpressionType NodeType => ExpressionType.Extension;

    public sealed override Expression Reduce() =>
        Content.AddPrologue(false, Prologue).AddEpilogue(false, Epilogue);

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        if (Content is Statement stmt)
            return stmt.VisitChildren(visitor);

        var expression = visitor.Visit(Content);
        return ReferenceEquals(expression, Content) ? this : new Statement(expression, Prologue, Epilogue);
    }

    public override bool CanReduce => true;
}