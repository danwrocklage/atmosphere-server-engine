using System.Runtime.CompilerServices;
using ACore.Abstractions;
using ACore.Modules;
using AUtils.IoC;

[assembly:InternalsVisibleTo("ACore.Patching.Tests")]

namespace ACore.Patching;

[Modules.Order(int.MaxValue)]
public class PatchingModule : Modules.Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Transient<PatchService, IPatchService>();
        builder.RegisterBy<Patch>(RegisterMode.AsSelf);
    }

    [RoleAny(Cell.SEED)]
    public async Task Run(CancellationToken token = default)
    {
        var destination = Services.Resolve<IConfiguration>().Get<string>("patch");
        
        var role = Services.Resolve<ICellEnvironment>().Role;
        if(!string.IsNullOrEmpty(destination))
            await Services.Resolve<IPatchService>().Migrate(role, destination);
        else
            await Services.Resolve<IPatchService>().Migrate(role);
    }
}