using System.Linq.Expressions;

namespace ACore.VisualScript.Models;

public record NodeLink(NodeUnit Unit, string ConnectionName);

public class NodeUnit
{
    /// <summary>
    /// Previous nodes (if it has)
    /// </summary>
    public NodeUnit[]? FlowInputs { get; init; }

    /// <summary>
    /// Node data dependencies
    /// </summary>
    public IReadOnlyDictionary<string, NodeLink> DataInputs { get; init; }

    /// <summary>
    /// Node input default values
    /// </summary>
    public IReadOnlyDictionary<string, Expression> DataValues { get; init; }

    /// <summary>
    /// Next nodes (if it has)
    /// </summary>
    public IDictionary<string, NodeUnit> FlowOutputs { get; } = new Dictionary<string, NodeUnit>();
        
    /// <summary>
    /// Result of processing current node
    /// </summary>
    public IReadOnlyDictionary<string, Expression>? DataOutput { get; set; }
        
    /// <summary>
    /// Node description from database
    /// </summary>
    internal NodeUnitDescription Description { get; init; }
        
    /// <summary>
    /// Current stack for this node
    /// </summary>
    internal NodeStack? NodeStack { get; set; }
        
    internal short Order { get; set; }

    public override string ToString() => 
        $"NodeUnit:'{Description?.Type}'";
}