using ACore.Abstractions.Rpc;

namespace ACore.Abstractions;

/// <summary>
/// Notify staff about important events
/// </summary>
/// <remarks>Send through <see cref="IRpc"/></remarks>
[Topic(RpcType.Fanout)]
public class GlobalNotificationEvent
{
    /// <summary>
    /// Default type of event
    /// </summary>
    public const string SYSTEM_TYPE = "system";

    /// <summary>
    /// Type of event
    /// </summary>
    public string Type { get; set; } = SYSTEM_TYPE;
    
    /// <summary>
    /// "Subtype" of event
    /// </summary>
    public string Channel { get; set; }
    
    /// <summary>
    /// Event body
    /// </summary>
    public string Message { get; set; }
}