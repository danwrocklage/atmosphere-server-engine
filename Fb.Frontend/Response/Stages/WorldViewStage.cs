using AGame.Actors.Replication;
using AGame.Transform;
using AUtils.Math;

namespace Fb.Frontend.Response.Stages;

internal class WorldViewStage : IResponseStage
{
    private readonly IActorProperties mProperties;
    private readonly ITransformService mTransformService;

    public WorldViewStage(ITransformService transformService, IActorProperties properties)
    {
        mTransformService = transformService;
        mProperties = properties;
    }

    public async Task<object> Execute(PlayerSession session, CancellationToken token = default)
    {
        var distanceOfView = 100f;
        var avatar = await session.CharacterAvatar();
        
        var position =  await (await avatar.Get<TransformComponent>(token: token))
            .Rpc(x => x.Position, token);
        var surroundActors = mTransformService
            .GetByRect(position, distanceOfView, distanceOfView);

        var items = new List<WorldViewItem>(surroundActors.Count);
        foreach (var surroundActor in surroundActors)
        {
            if (surroundActor.Key == avatar.Id)
                continue;
            
            items.Add(new WorldViewItem
            {
                Position = surroundActor.Value,
                Direction = mProperties.Get<Point3>(surroundActor.Key, "transform.direction"),
                Mesh = mProperties.Get<string>(surroundActor.Key, "visualstate.mesh"),
                State = mProperties.Get<string>(surroundActor.Key, "visualstate.state"),
                Id = surroundActor.Key,
                Cached = false,
                MorphTargets = null
            });
        }

        return new WorldViewDto {Items = items.ToArray()};
    }
}