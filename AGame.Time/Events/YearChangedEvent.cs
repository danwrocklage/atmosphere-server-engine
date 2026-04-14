using ACore.Abstractions.Rpc;
using AUtils.Sil;

namespace AGame.Time.Events;

[Topic(RpcTopics.YEAR_CHANGED, RpcType.Fanout)]
[Sil(118)]
internal struct YearChangedEvent
{
    public uint Year { get; set; }
}