using ACore.Abstractions;
using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ACore.Telegram;

[Log(Category = "[ex] telegram")]
internal class TelegramClient : IAsyncInitializable, IDisposable
{
    private TelegramBotClient mClient;
    private bool mIsEnabled;
    private readonly CancellationTokenSource mCancellationTokenSource;

    private readonly IConfiguration mConfiguration;
    private readonly ILogger<TelegramClient> mLogger;
    private readonly IRepository<TelegramChatEntity> mChats;

    public TelegramClient(IConfiguration configuration, IDatabase database, ILogger<TelegramClient> logger)
    {
        mChats = database.Repository<TelegramChatEntity>();
        mConfiguration = configuration;
        mLogger = logger;
        mCancellationTokenSource = new CancellationTokenSource();
    }

    public async Task SendMessage(string message, CancellationToken cancellationToken = default)
    {
        if(!mIsEnabled)
            return;

        var chats = await mChats.Select()
            .Select(x => x.ChatId)
            .ToListAsync(cancellationToken);

        foreach (var chat in chats)
            await mClient.SendMessage(
                new ChatId(chat), message, 
                ParseMode.Html, 
                cancellationToken: cancellationToken);
    }

    public async Task InitializeAsync()
    {
        var config = mConfiguration.Get(() => TelegramClientConfig.Default);
        if (string.IsNullOrEmpty(config.Token))
        {
            mLogger.Warn("Telegram notifications is disabled. API token is not provided");
            mIsEnabled = false;
            return;
        }
        
        mClient = new TelegramBotClient(config.Token);
        mIsEnabled = await mClient.TestApi();

        if (mIsEnabled)
            mClient.StartReceiving(
                new MembersUpdateHandler(mChats, config.HelloKey, mLogger),
                new ReceiverOptions {AllowedUpdates = new[] {UpdateType.Message}},
                mCancellationTokenSource.Token);
    }
    
    public void Dispose()
    {
        mCancellationTokenSource.Cancel();
        mCancellationTokenSource.Dispose();
    }

    #region Utils

    [Configuration("telegram")]
    private class TelegramClientConfig
    {
        public string Token { get; set; }
        
        public string HelloKey { get; set; }

        public static TelegramClientConfig Default => new()
        {
            Token = "",
            HelloKey = "Atmosphere Engine Hello"
        };
    }

    #endregion
}