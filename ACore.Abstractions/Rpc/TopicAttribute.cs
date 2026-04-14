namespace ACore.Abstractions.Rpc;

/// <summary>
/// Type of RPC handling
/// </summary>
public enum RpcType : byte
{
    /// <summary>
    /// First available subscriber will receive message
    /// </summary>
    /// <remarks>Default type for handler</remarks>
    Group,
    
    /// <summary>
    /// All subscribers receive message
    /// </summary>
    Fanout,
    
    /// <summary>
    /// First available subscriber will receive message with reply required
    /// </summary>
    Request
}

/// <summary>
/// Attribute for set topic name
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class TopicAttribute : Attribute
{
    public TopicAttribute(string topic)
    {
        Topic = topic;
    }
    
    public TopicAttribute(string topic, RpcType type)
    {
        Topic = topic;
        Type = type;
    }
    
    public TopicAttribute(RpcType type)
    {
        Type = type;
    }

    /// <summary>
    /// Topic name
    /// </summary>
    public string Topic { get; }
    
    /// <summary>
    /// Type of RPC handling
    /// </summary>
    public RpcType Type { get; }
}