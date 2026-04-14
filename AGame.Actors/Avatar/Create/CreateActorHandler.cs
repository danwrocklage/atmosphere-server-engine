using System.Diagnostics.CodeAnalysis;
using ACore.Abstractions;
using ACore.Abstractions.Rpc;
using AGame.Actors.Avatar;

namespace AGame.Actors.Handlers;

[SuppressMessage("ReSharper", "UnusedType.Global")]
internal class CreateActorHandler : IRpcHandler<CreateActorRequest>
{
    private readonly ActorContainer mActorContainer;

    public CreateActorHandler(ActorContainer actorContainer)
    {
        mActorContainer = actorContainer;
    }

    public async Task Handle(IRpcContext<CreateActorRequest> context, CancellationToken token = default)
    {
        var actorType = ActorTypeCache.Get(context.Message.Type);
        if (actorType == null)
        {
            context.Reply(new CreateActorResponse {IsSuccess = false});
            return;
        }

        var actor = await mActorContainer.CreateActor(context.Message.ActorId, actorType, context.Message.Name, context.Message.IsThin,
            context.Message.ParentId);
        context.Reply(new CreateActorResponse
        {
            ActorId = actor?.Id ?? Guid.Empty,
            CellId = Cell.AppId,
            IsSuccess = actor?.Id != null && actor?.Id != Guid.Empty
        });
    }
}