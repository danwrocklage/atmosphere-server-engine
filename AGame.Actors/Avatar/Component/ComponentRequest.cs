using ACore.Abstractions.Rpc;
using AUtils.Sil;

namespace AGame.Actors.Avatar;

[Sil(122)]
[Topic(RpcTopics.ACTOR_COMPONENT, RpcType.Request)]
internal class ComponentRequest
{
    public Guid ActorId { get; set; }
    
    public RpcComponent Component { get; set; }
    
    public ComponentRequestType Type { get; set; }
}

internal enum ComponentRequestType
{
    Create,
    Remove,
    Get
}