using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace ACore.Telegram;

internal class MembersUpdateHandler : IUpdateHandler
{
    private readonly IRepository<TelegramChatEntity> mChats;
    private readonly string mHelloKey;
    private readonly ILogger<TelegramClient> mLogger;

    public MembersUpdateHandler(IRepository<TelegramChatEntity> chats, string helloKey,
        ILogger<TelegramClient> logger)
    {
        mChats = chats ?? throw new ArgumentNullException(nameof(chats));
        mHelloKey = helloKey ?? throw new ArgumentNullException(nameof(helloKey));
        mLogger = logger;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if(string.IsNullOrEmpty(update.Message?.Text))
            return;

        if (update.Message.Text == $"/hello {mHelloKey}")
            await mChats.Insert(new TelegramChatEntity
            {
                Id = Guid.NewGuid(), 
                ChatId = update.Message.Chat.Id,
                CreatedAt = DateTime.UtcNow
            });

        if (update.Message.Text == "/bye")
            await mChats.Delete(x => x.ChatId == update.Message.Chat.Id);
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        mLogger.Warn("Error on polling from telegram api", exception);
        return Task.CompletedTask;
    }
}