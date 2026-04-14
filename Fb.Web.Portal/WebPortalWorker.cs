using ACore.Abstractions.Worker;
using ACore.Worker.Web;
using AUtils.IoC;

namespace Fb.Web.Portal;

[Worker("web-portal")]
internal class WebPortalWorker : WebWorker
{
    public WebPortalWorker(IContainer container) : base(container)
    {
    }

    protected override void Configure(PipelineBuilder builder)
    {
        builder.UseRoutingInfo();
        builder.UseMiddleware<AccountAuthenticateMiddleware>();
        builder.UseAssemblyControllers();
    }
}