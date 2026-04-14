//#if Development

using ACore.Abstractions.Storage;
using AGame.Actors.Avatar;
using AGame.Frontend;
using AGame.Transform;
using AUtils.Math;
using AUtils.Sil;
using Fb.Frontend.Response;
using Fb.Mechanics;
using Fb.Mechanics.Developer;
using Fb.Mechanics.PlayerCharacter;

namespace Fb.Frontend.Developer;

[Sil(1001)]
public struct CreateBoxesRequest
{
    public static readonly object Instance = new CreateBoxesRequest();
}

public class CreateBoxesAroundHandler : PipelineHandler<CreateBoxesRequest>
{
    private readonly AvatarContext mAvatarContext;
    private readonly IStorage mStorage;
    private readonly StateResponseService mResponseService;

    public CreateBoxesAroundHandler(AvatarContext avatarContext, IStorage storage, 
        StateResponseService responseService)
    {
        mAvatarContext = avatarContext;
        mStorage = storage;
        mResponseService = responseService;
    }

    protected override async Task<object> Handle(CreateBoxesRequest message, PipelineHandlerContext context)
    {
        var characterId = await mStorage.Get<Guid>($"player:{context.EntityId}:character");
        var characterAvatar = await mAvatarContext.Get<PlayerCharacterActor>(characterId);

        var position = await (await characterAvatar.Get<TransformComponent>())
            .Rpc(x => x.Position);

        var boxPositions = new[]
        {
            new Point3(position.X + 40, position.Y, position.Z),
            new Point3(position.X - 40, position.Y, position.Z),
            new Point3(position.X, position.Y + 40, position.Z),
            new Point3(position.X, position.Y - 40, position.Z)
        };

        foreach (var boxPosition in boxPositions)
        {
            var box = await mAvatarContext.Create<BoxActor>();
            await (await box.Get<TransformComponent>(token: context.CancellationToken))
                .Rpc(x => x.Position, boxPosition, context.CancellationToken);

            var visualState = await box.Get<VisualStateComponent>();
            await visualState.Rpc(x => x.Mesh, "Box", context.CancellationToken);
            await visualState.Rpc(x => x.State, "Idle", context.CancellationToken);
        }

        return await mResponseService.Process(context.EntityId, context.CancellationToken);
    }
}