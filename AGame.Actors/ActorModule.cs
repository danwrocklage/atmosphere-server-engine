using System.Runtime.CompilerServices;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Rpc;
using ACore.Abstractions.Worker;
using ACore.Modules;
using AGame.Actors.Avatar;
using AGame.Actors.Eventing;
using AGame.Actors.Handlers;
using AGame.Actors.Replication;
using AUtils.IoC;

[assembly:InternalsVisibleTo("AGame.Time.Module")]

namespace AGame.Actors;

[Order(1)]
public class ActorModule : Module, IAsyncDisposable
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.RegisterBy<Actor>(RegisterMode.AsSelf);
        builder.RegisterBy<ActorComponent>(RegisterMode.AsSelf);
        
        builder.Singleton<ActorContainer, ActorContainer, IAsyncInitializable>();
        builder.Singleton<ActorEventer, ActorEventer, IActorEventer>();
        builder.Singleton<ActorPropertyStorage, ActorPropertyStorage, IActorProperties>();
        builder.Transient<ActorTickWorker>();
        builder.Singleton<AvatarContext>();
        
        builder.Transient<ActorPropertyHandler, IRpcHandler<ActorProperty>>();
        builder.Transient<ActorEventHandler, IRpcHandler<ActorEvent>>();
        builder.Transient<CreateActorHandler, IRpcHandler<CreateActorRequest>>();
        builder.Transient<DestroyActorHandler, IRpcHandler<DestroyRequest>>();
        builder.Transient<RpcActorHandler, IRpcHandler<RpcRequest>>();

        builder.Transient<ActorCountEventHandler, IRpcHandler<ActorCountEvent>>();
    }

    [RoleAny(Cell.MECHANICS)]
    public Task RunMechanics(CancellationToken token = default)
    {
        Subscribe<CreateActorRequest>(RpcTopics.ACTOR_CREATE);
        Subscribe<ComponentRequest>(RpcTopics.ACTOR_COMPONENT);
        Subscribe<DestroyRequest>(RpcTopics.ACTOR_DESTROY);
        Subscribe<RpcRequest>($"{RpcTopics.ACTOR_RPC}.{Cell.AppId}");
        Subscribe<ActorEvent>(RpcTopics.ALL_EVENTS);
        Subscribe<ActorCountEvent>($"{RpcTopics.ACTOR_COUNT}.{Cell.AppId}");
        
        Worker<ActorTickWorker>(token);
        
        return Task.CompletedTask;
    }

    [RoleAny]
    public Task RunReplication(CancellationToken token = default)
    {
        Subscribe<ActorProperty>();

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        var actorContainer = Services?.Resolve<ActorContainer>();
        if(actorContainer != null)
        {
            Services?.Resolve<ILogger<ActorContainer>>().Info("Storing actors...");
            await actorContainer.StoreActors();
        }
    }
}