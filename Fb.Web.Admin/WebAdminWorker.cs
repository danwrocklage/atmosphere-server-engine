using ACore.Abstractions.Worker;
using ACore.Worker.Web;
using AUtils.IoC;

namespace Fb.Web.Admin;

[Worker("web-admin")]
public class WebAdminWorker : WebWorker
{
    public WebAdminWorker(IContainer container) : base(container)
    {
    }

    protected override void Configure(PipelineBuilder builder)
    {
        builder.UseRoutingInfo();
        builder.UseMiddleware<AdminAuthenticateMiddleware>();
        builder.UseAssemblyControllers();
    }
}