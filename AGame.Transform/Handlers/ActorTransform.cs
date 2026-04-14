using ACore.Abstractions.Rpc;
using AUtils.Math;
using AUtils.Sil;

namespace AGame.Transform;

[Sil(123)]
[Topic("actor.transform", RpcType.Fanout)]
internal struct ActorTransform
{
    public Guid ActorId { get; set; }

    public Point3 Position { get; set; }
}

[Sil(124)]
[Topic("actor.transform.remove", RpcType.Fanout)]
internal struct ActorTransformRemove
{
    public Guid ActorId { get; set; }
}