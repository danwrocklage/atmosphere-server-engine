using System.Runtime.CompilerServices;
using ACore.Worker.Web.Routing;
using AUtils.IoC;

[assembly:InternalsVisibleTo("ACore.Worker.Web.Tests")]

namespace ACore.Worker.Web;

[Modules.Order(1)]
public class WebWorkerModule : ACore.Modules.Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Singleton<RouteManager>();
        builder.Transient<Pipeline>();
        builder.Transient<Router>();
        builder.Transient<PipelineBuilder>();
            
        builder.RegisterBy<Module>();
        builder.RegisterBy<Middleware>();
        builder.RegisterBy<Controller>();
    }
}