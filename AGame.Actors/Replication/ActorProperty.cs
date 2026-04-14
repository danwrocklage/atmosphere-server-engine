using ACore.Abstractions.Rpc;
using AUtils.Sil;

namespace AGame.Actors.Replication;

[Sil(151)]
[Topic(RpcTopics.ACTOR_REPLICATION, RpcType.Fanout)]
internal class ActorProperty
{
    public Guid ActorId { get; set; }
    
    public string Property { get; set; }
    
    public object Value { get; set; }
}