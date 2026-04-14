using ACore.Abstractions.Rpc;
using AGame.Actors.Avatar;

namespace AGame.Actors.Handlers;

internal class DestroyActorHandler : IRpcHandler<DestroyRequest>
{
    private readonly ActorContainer mActorContainer;

    public DestroyActorHandler(ActorContainer actorContainer)
    {
        mActorContainer = actorContainer;
    }

    public async Task Handle(IRpcContext<DestroyRequest> context, CancellationToken token = default)
    {
        var actor = mActorContainer.GetActor(context.Message.ActorId);
        if(actor == null)
            return;
        
        await mActorContainer.DestroyActor(actor);
    }
}