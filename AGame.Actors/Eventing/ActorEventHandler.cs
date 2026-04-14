using ACore.Abstractions.Rpc;

namespace AGame.Actors.Eventing;

/// <summary>
/// Handle any actor event and route it
/// </summary>
internal class ActorEventHandler : IRpcHandler<ActorEvent>
{
    private readonly ActorContainer mActorContainer;
    private readonly ActorEventer mActorEventer;

    public ActorEventHandler(ActorContainer actorContainer, ActorEventer actorEventer)
    {
        mActorContainer = actorContainer;
        mActorEventer = actorEventer;
    }

    public Task Handle(IRpcContext<ActorEvent> context, CancellationToken token = default)
    {
        var eventType = context.Message.Type;
        var payload = context.Message.Payload;
        var actorId = context.Message.SenderActorId;
        
        switch (eventType)
        {
            case ActorEventType.Create:
            {
                if(actorId.HasValue)
                    mActorEventer.OnCreateActor(actorId.Value);
                break;
            }
            case ActorEventType.Delete:
            {
                if(actorId.HasValue)
                    mActorEventer.OnDestroyActor(actorId.Value);
                break;
            }
            case ActorEventType.Event:
            {
                if (context.Message.TargetActorIds is {Length: > 0})
                {
                    foreach (var targetActorId in context.Message.TargetActorIds)
                        mActorContainer.GetActor(targetActorId)?.ReceiveEvent(payload);
                    break;
                }
                
                foreach (var actor in mActorContainer.Actors)
                    actor.ReceiveEvent(payload);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(context.Message.Type));
        }
        
        return Task.CompletedTask;
    }
}