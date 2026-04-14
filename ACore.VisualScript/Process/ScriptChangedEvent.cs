using ACore.Abstractions.Rpc;

namespace ACore.VisualScript;

[Topic("script.changed", RpcType.Fanout)]
internal struct ScriptChangedEvent
{
    public Guid ScriptId { get; set; }
}