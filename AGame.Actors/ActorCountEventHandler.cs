using ACore.Abstractions.Rpc;
using AUtils.Sil;

namespace AGame.Actors;

[Sil(148)]
[Topic(RpcType.Request)]
public struct ActorCountEvent {}

internal class ActorCountEventHandler : IRpcHandler<ActorCountEvent>
{
    private readonly ActorContainer mActorContainer;

    public ActorCountEventHandler(ActorContainer actorContainer)
    {
        mActorContainer = actorContainer;
    }

    public Task Handle(IRpcContext<ActorCountEvent> context, CancellationToken token = default)
    {
        context.Reply(mActorContainer.Actors.Count);
        return Task.CompletedTask;
    }
}