using ACore.Abstractions;
using ACore.Abstractions.Rpc;
using ACore.Modules;
using AUtils.IoC;

namespace AGame.Transform;

[Order(2)]
public class ActorTransformModule : Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Transient<ActorTransformHandler, IRpcHandler<ActorTransform>, IRpcHandler<ActorTransformRemove>>();
        builder.Register(x => x.For<TransformService>().As<ITransformService>().As<ITransformUpdater>().AsSelf().Singleton());
    }

    [RoleAny(Cell.MECHANICS, Cell.FRONTEND)]
    public Task Start(CancellationToken token = default)
    {
        Subscribe<ActorTransform>();
        Subscribe<ActorTransformRemove>();
        return Task.CompletedTask;
    }
}