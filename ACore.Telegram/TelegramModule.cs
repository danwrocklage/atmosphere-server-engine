using ACore.Abstractions;
using ACore.Abstractions.Rpc;
using AUtils.IoC;

namespace ACore.Telegram;

public class TelegramModule : ACore.Modules.Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Singleton<TelegramClient>();
        builder.Transient<SystemNotificationHandler, IRpcHandler<GlobalNotificationEvent>>();
    }

    public override Task Run(IContainer container, CancellationToken token = default)
    {
        container.Resolve<IRpcSubscribe>().Subscribe<GlobalNotificationEvent>();
        return Task.CompletedTask;
    }
}