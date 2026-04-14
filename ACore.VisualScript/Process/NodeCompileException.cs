using ACore.VisualScript.Models;

namespace ACore.VisualScript;

public class NodeCompileException : Exception
{
    public NodeUnit? Node { get; }

    public NodeCompileException(string? message, NodeUnit? node, Exception? innerException = null) 
        : base(message, innerException)
    {
        Node = node;
    }
}