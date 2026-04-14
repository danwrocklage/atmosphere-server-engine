using ACore.Modules;
using AUtils.IoC;

namespace Fb.Seed;

public class SeedModule : Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Transient<SeedWorker>();
    }
}