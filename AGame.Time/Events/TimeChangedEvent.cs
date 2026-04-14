using ACore.Abstractions.Rpc;
using AUtils.Sil;

namespace AGame.Time.Events;

[Topic(RpcTopics.TIME_CHANGED, RpcType.Fanout)]
[Sil(117)]
internal struct TimeChangedEvent
{
    public TimeOfDay TimeOfDay { get; set; }
}