using ACore.Abstractions.Rpc;
using AGame.Actors.Replication;
using AUtils.IoC;

namespace AGame.Actors;

internal class ActorContext
{
    public ActorContext(IContainer container)
    {
        Container = container;
        Actors = container.Resolve<ActorContainer>();
        Rpc = container.Resolve<IRpc>();
        ReplicationStorage = container.Resolve<ActorPropertyStorage>();
    }
    
    public ActorContainer Actors { get; }

    public IContainer Container { get; }
    
    public IRpc Rpc { get; }
    
    public ActorPropertyStorage ReplicationStorage { get; }
}