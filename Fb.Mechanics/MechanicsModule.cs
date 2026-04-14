using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Modules;
using AGame.Actors;
using AGame.Actors.Avatar;
using AGame.Actors.Persistence;
using AUtils.IoC;
using AUtils.Sil;
using Fb.Mechanics.PlayerCharacter;

namespace Fb.Mechanics;

[Sil(1021)]
public class TestEmit
{
    public string TestValue { get; set; }
}

public class TestActor : Actor
{
    [Persistence] private string mTestString;
    private readonly ILogger<TestActor> mLogger;

    public TestActor(ILogger<TestActor> logger)
    {
        mLogger = logger;
        IsEventReceiver = true;
        Watch<TestEmit>(e =>
        {
            mTestString = e.TestValue;
            mLogger.Info($"Get new value {e.TestValue}");
        });
    }
}

public class TestActorEmitter : Actor
{
    private TimeSpan mElapsed;

    public TestActorEmitter()
    {
        TickingMode = TickingMode.ActorTickingOnly;
    }

    protected override void OnTick(TimeSpan delta)
    {
        mElapsed += delta;
        
        if(mElapsed < TimeSpan.FromSeconds(2))
            return;
        
        mElapsed -= TimeSpan.FromSeconds(2);
        Emit(new TestEmit {TestValue = $"Some emit value {mElapsed}"});
    }
}

[Order(1)]
public class MechanicsModule : Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Transient<CharacterCreationService, ICharacterCreationService>();
    }

    [RoleAny(Cell.MECHANICS)]
    public async Task RunMechanics(CancellationToken token = default)
    {
        //var context = container.Resolve<AvatarContext>();
        //await context.Create<TestActor>();
        //await context.Create<TestActorEmitter>();
    }
}