using ACore.Abstractions.Rpc;
using AUtils.Sil;

namespace AGame.Actors.Avatar;

[Sil(110)]
[Topic(RpcType.Request)]
internal class CreateActorRequest
{
    public Guid? ActorId { get; set; }
    
    public Guid? ParentId { get; set; }
    
    public bool IsThin { get; set; }
    
    public string Type { get; set; }
    
    public string Name { get; set; }
}