using ACore.Abstractions;
using ACore.Abstractions.Logging;

namespace AGame.Frontend;

internal class ConnectionEnableService
{
    private readonly ILogger<ConnectionEnableService> mLogger;
    private bool mIsEnable;
    
    public ConnectionEnableService(ILogger<ConnectionEnableService> logger, IConfiguration configuration)
    {
        mLogger = logger;
        mIsEnable = configuration.Get("frontend.enable", () => true);
    }

    public bool IsEnable
    {
        get => mIsEnable;
        set
        {
            if(mIsEnable != value)
                mLogger.Info($"Frontend connections have been {(value ? "enabled" : "disabled")}");

            mIsEnable = value;
        }
    }
}