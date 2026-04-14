using ACore.Modules;
using AUtils.IoC;
using Fb.Frontend.Response;

namespace Fb.Frontend;

public class FrontendModule : Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Transient<PlayerSession>();
        builder.RegisterBy<IResponseStage>(RegisterMode.AsTarget);
        
        builder.Singleton<AfkTracker, IAfkTracker>();
    }
}