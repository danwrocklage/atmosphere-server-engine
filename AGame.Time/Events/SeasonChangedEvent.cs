using ACore.Abstractions.Rpc;
using AUtils.Sil;

namespace AGame.Time.Events;

[Topic(RpcTopics.SEASON_CHANGED, RpcType.Fanout)]
[Sil(116)]
internal struct SeasonChangedEvent
{
    public Season Season { get; set; }
}