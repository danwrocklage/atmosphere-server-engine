using ACore.Abstractions.Rpc;

namespace AGame.Transform;

internal class ActorTransformHandler : IRpcHandler<ActorTransform>, IRpcHandler<ActorTransformRemove>
{
    private readonly TransformService mTransformService;

    public ActorTransformHandler(TransformService transformService)
    {
        mTransformService = transformService;
    }

    public Task Handle(IRpcContext<ActorTransform> context, CancellationToken token = default)
    {
        mTransformService.InternalUpdate(context.Message.ActorId, context.Message.Position);
        return Task.CompletedTask;
    }

    public Task Handle(IRpcContext<ActorTransformRemove> context, CancellationToken token = default)
    {
        mTransformService.InternalRemove(context.Message.ActorId);
        return Task.CompletedTask;
    }
}