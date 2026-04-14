using System.Linq.Expressions;
using System.Text.Json;
using ACore.VisualScript.Models;

namespace ACore.VisualScript;

internal class NodeConverter
{
    internal const string ANY_DATA_TYPE = "any";
        
    private static readonly HashSet<string> sAllowedSystemTypes = new()
    {
        "System.String",
        "System.Int32",
        "System.Int16",
        "System.Int64",
        "System.Guid",
        "System.DateTime",
        "System.Boolean",
        "System.UInt32",
        "System.UInt16",
        "System.UInt64",
        "System.Byte",
        "System.SByte",
        "System.Single",
        "System.Double",
        "System.Char",
        "System.Decimal",
        "ACore.Abstractions.Logging.LogLevel",
            
        "AGame.Gameplay.GameTime"
    };

    private bool mIsConverted;
    private readonly IReadOnlyCollection<ScriptItem> mSourceNodes;
    private readonly IReadOnlyDictionary<string, NodeUnitDescription> mDescriptions;
    private readonly Dictionary<string, NodeUnit> mResultUnits;

    public NodeConverter(
        IReadOnlyCollection<ScriptItem> sourceNodes,
        IReadOnlyDictionary<string, NodeUnitDescription> descriptions)
    {
        mIsConverted = false;
        mSourceNodes = sourceNodes;
        mDescriptions = descriptions;
        mResultUnits = new Dictionary<string, NodeUnit>(sourceNodes.Count);
    }

    public IReadOnlyDictionary<string, NodeUnit> Result =>
        mIsConverted ? mResultUnits : throw new InvalidOperationException();

    public void Convert()
    {
        foreach (var sourceNode in mSourceNodes)
            GetUnit(sourceNode);

        mIsConverted = true;
    }

    private NodeUnit GetUnit(ScriptItem? schemaItem)
    {
        if (schemaItem == null)
            throw new ArgumentNullException(nameof(schemaItem));

        if (mResultUnits.ContainsKey(schemaItem.Id))
            return mResultUnits[schemaItem.Id];

        var (units, links) = GetFlowInputs(schemaItem);

        var unit = new NodeUnit
        {
            DataValues = GetDataValues(schemaItem),
            DataInputs = GetDataInputs(schemaItem),
            FlowInputs = units,
            Description = mDescriptions[schemaItem.Type]
        };

        mResultUnits.Add(schemaItem.Id, unit);

        foreach (var (scriptNodeUnit, connectionName) in links)
            scriptNodeUnit.FlowOutputs.Add(connectionName, unit);

        return unit;
    }

    private Dictionary<string, Expression> GetDataValues(ScriptItem? schemaItem)
    {
        if (schemaItem?.Values == null)
            return new Dictionary<string, Expression>();

        var types = mDescriptions[schemaItem.Type].Input
            .Where(x => schemaItem.Values.ContainsKey(x.Name) && !x.IsFlow)
            .DistinctBy(x => x.Name)
            .ToDictionary(x => x.Name, x => x.Type);

        if (types.Values.Any(x => !x.StartsWith("s:") || !sAllowedSystemTypes.Contains(x[2..])))
            throw new NodeCompileException($"{nameof(ScriptItem.Values)} must contains only system types values", null);
            
        return schemaItem.Values.ToDictionary(x => x.Key, x =>
        {
            if (types[x.Key] == ANY_DATA_TYPE)
                throw new NodeCompileException("Type 'any' is forbidden for data values", null);
                
            if(types[x.Key] == "s:System.String")
                return (Expression)Expression.Constant(x.Value);

            var type = Type.GetType(types[x.Key][2..]) ?? 
                       ACore.Abstractions.Types.All.FirstOrDefault(t => t.FullName == types[x.Key][2..]) ??
                       throw new InvalidOperationException($"Type '{types[x.Key][2..]}' was not found");
            var value = type.IsEnum ? Enum.Parse(type, x.Value) : JsonSerializer.Deserialize(x.Value, type);
            return Expression.Constant(value, type);
        });
    }

    private (NodeUnit[] Units, NodeLink[] Links) GetFlowInputs(ScriptItem schemaItem)
    {
        var units = new List<NodeUnit>();
        var links = new List<NodeLink>();

        foreach (var x in mSourceNodes)
        {
            var outConnection = x.Connections
                .SingleOrDefault(a => a.IsFlow && a.IsOutput && a.NodeId == schemaItem.Id);

            if (outConnection == null)
                continue;

            var xItem = GetUnit(x);
            units.Add(xItem);
            links.Add(new NodeLink(xItem, outConnection.Name));
        }

        return (units.ToArray(), links.ToArray());
    }

    private Dictionary<string, NodeLink> GetDataInputs(ScriptItem schemaItem)
    {
        var dataInputs = new Dictionary<string, NodeLink>();
        foreach (var connection in schemaItem.Connections)
        {
            if (connection.IsFlow || connection.IsOutput)
                continue;

            var linkedNode = mSourceNodes.FirstOrDefault(x => x.Id == connection.NodeId);
            if (linkedNode == null)
                throw new InvalidOperationException(
                    $"ID: {connection.NodeId} from connection in {schemaItem.Id} not found");

            dataInputs.Add(connection.Name, new NodeLink(GetUnit(linkedNode), connection.EndpointName));
        }

        return dataInputs;
    }
}