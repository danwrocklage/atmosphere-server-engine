using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Rpc;
using ACore.Modules;
using AGame.Actors.Avatar;
using AGame.Time.Events;
using AUtils.IoC;

namespace AGame.Time;

[ACore.Modules.Order(2)]
public class GameTimeModule : ACore.Modules.Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Register(x => x.For<GameTimeService>()
            .As<IGameTimeService>()
            .As<IRpcHandler<TimeChangedEvent>>()
            .As<IRpcHandler<SeasonChangedEvent>>()
            .As<IRpcHandler<YearChangedEvent>>()
            .Singleton()
        );
    }

    [RoleAny(Cell.MECHANICS)]
    public async Task RunMechanic(CancellationToken token = default)
    {
        var context = Services.Resolve<AvatarContext>();
        var avatar = await context.Get<GameTimeActor>(GameTimeService.ActorIdSingleton);
        if (avatar.IsEmpty)
        {
            avatar = await context.Create<GameTimeActor>(GameTimeService.ActorIdSingleton, name: "WorldTime", token: token);
            if (avatar.IsEmpty)
                Services.Resolve<ILogger<GameTimeActor>>().Warn("Failed to create game time actor");    
        }
    }
}