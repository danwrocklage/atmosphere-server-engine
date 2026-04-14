using ACore.Abstractions.Rpc;
using AUtils.Sil;

namespace AGame.Actors.Avatar;

[Sil(113)]
[Topic(RpcType.Request)]
internal struct DestroyRequest
{
    public Guid ActorId { get; set; }
}