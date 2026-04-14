using System.Runtime.CompilerServices;
using ACore.Abstractions;
using ACore.Modules;
using AUtils.IoC;

[assembly:InternalsVisibleTo("Fb.Frontend.Bot")]

namespace AGame.Frontend;

public class FrontendModule : Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Transient<FrontendServerWorker>();
        builder.Transient<ConnectionPipeline>();
        builder.Singleton<ConnectionEnableService>();
        builder.RegisterBy(typeof(PipelineHandler<>), RegisterMode.AsSelf);
    }

    [RoleAny(Cell.FRONTEND)]
    public Task RunFrontend(CancellationToken token = default)
    {
        ConnectionPipeline.Initialize();
        Worker<FrontendServerWorker>(CancellationToken.None);
        return Task.CompletedTask;
    }
}