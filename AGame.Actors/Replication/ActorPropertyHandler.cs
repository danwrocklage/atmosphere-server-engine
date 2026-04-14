using ACore.Abstractions.Rpc;

namespace AGame.Actors.Replication;

internal class ActorPropertyHandler : IRpcHandler<ActorProperty>
{
    private readonly ActorPropertyStorage mStorage;

    public ActorPropertyHandler(ActorPropertyStorage storage)
    {
        mStorage = storage;
    }

    public Task Handle(IRpcContext<ActorProperty> context, CancellationToken token = default)
    {
        mStorage.Set(context.Message.ActorId, context.Message.Property, context.Message.Value);
        return Task.CompletedTask;
    }
}