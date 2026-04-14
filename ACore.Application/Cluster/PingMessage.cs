using ACore.Abstractions.Rpc;
using AUtils.Sil;

namespace ACore.Application.Cluster;

/// <summary>
/// Ping message
/// </summary>
[Sil(100)]
[Topic(RpcTopics.PING, RpcType.Fanout)]
internal class PingMessage
{
    /// <summary>
    /// Cell unique id
    /// </summary>
    public Guid Id { get; set; }
}