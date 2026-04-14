using ACore.Abstractions;
using ACore.Abstractions.Rpc;

namespace ACore.Telegram;

internal class SystemNotificationHandler : IRpcHandler<GlobalNotificationEvent>
{
    private readonly TelegramClient mTelegramClient;

    public SystemNotificationHandler(TelegramClient telegramClient)
    {
        mTelegramClient = telegramClient;
    }

    public async Task Handle(IRpcContext<GlobalNotificationEvent> context, CancellationToken token = default)
    {
        // Send only system notifications
        if(context.Message.Type != GlobalNotificationEvent.SYSTEM_TYPE ||
           string.IsNullOrEmpty(context.Message.Message))
            return;

        await mTelegramClient.SendMessage($"✨Atmosphere Engine:</br> {context.Message.Message}", token);
    }
}