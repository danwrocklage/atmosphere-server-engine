using ACore.Abstractions;
using ACore.Abstractions.Rpc;
using AUtils.IoC;

namespace ACore.Nats;

public class NatsModule : ACore.Modules.Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Register(x => x.For<NatsRpc>()
            .As<IRpc>()
            .As<IRpcSubscribe>()
            .As<IInitializable>()
            .Singleton());

        builder.RegisterBy(typeof(IRpcHandler<>));
    }
}