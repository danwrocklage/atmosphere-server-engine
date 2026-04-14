using ACore.Abstractions;
using ACore.Modules;
using AUtils.IoC;
using Fb.Web.Shared;

namespace Fb.Web.Portal;

[Order(1)]
public class WebPortalModule : Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.AddWebSharedServices();
        builder.Transient<WebPortalWorker>();
        builder.Transient<UnblockAccountsByPassword>();
        builder.Transient<RemoveRevokedTokensWorker>();
    }

    [RoleAny(Cell.PORTAL_API)]
    public Task RunPortal(CancellationToken token = default)
    {
        Worker<WebPortalWorker>(token);
        Worker<RemoveRevokedTokensWorker>("0 4 * * * *", token);
        Worker<UnblockAccountsByPassword>("1 * * * * *", token);
        
        return Task.CompletedTask;
    }
}