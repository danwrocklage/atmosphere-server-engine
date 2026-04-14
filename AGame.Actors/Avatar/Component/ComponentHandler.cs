using ACore.Abstractions.Rpc;
using AGame.Actors.Avatar;

namespace AGame.Actors.Handlers;

internal class ComponentHandler : IRpcHandler<ComponentRequest>
{
    private readonly ActorContainer mActorContainer;

    public ComponentHandler(ActorContainer actorContainer)
    {
        mActorContainer = actorContainer;
    }

    public Task Handle(IRpcContext<ComponentRequest> context, CancellationToken token = default)
    {
        if(context.Message.Component == null)
        {
            context.Reply(false);
            return Task.CompletedTask;
        }
        
        var actor = mActorContainer.GetActor(context.Message.ActorId);
        if(actor == null)
        {
            context.Reply(false);
            return Task.CompletedTask;
        }
        var componentType = Type.GetType(context.Message.Component.Type);
        if (componentType == null)
        {
            context.Reply(false);
            return Task.CompletedTask;
        }
        
        switch (context.Message.Type)
        {
            case ComponentRequestType.Remove:
                actor.Remove(componentType, context.Message.Component.Name);
                context.Reply(true);
                break;
            case ComponentRequestType.Create:
                actor.Add(componentType, context.Message.Component.Name);
                context.Reply(true);
                break;
            case ComponentRequestType.Get:
                context.Reply(actor.Has(componentType, context.Message.Component.Name));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return Task.CompletedTask;
    }
}