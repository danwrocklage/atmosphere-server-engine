using ACore.Abstractions.Rpc;
using AUtils.Sil;

namespace AGame.Actors.Avatar;

[Sil(112)]
[Topic(RpcType.Request)]
internal class RpcRequest
{
    public Guid ActorId { get; set; }
    
    public string Method { get; set; }
    
    public object[] Arguments { get; set; }
    
    public RpcComponent Component { get; set; }
}

[Sil(121)]
internal class RpcComponent
{
    public string Type { get; set; }
    
    public string Name { get; set; }
}