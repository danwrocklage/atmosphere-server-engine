using System.Collections.Specialized;
using System.ComponentModel;
using ACore.Abstractions;
using ACore.Abstractions.Logging;

namespace ACore.Application.Commands.Handlers;

[DisplayName("log.level")]
internal class ChangeLogLevelHandler : ICommandHandler
{
    private readonly ILoggerManager mLogger;

    public ChangeLogLevelHandler(ILoggerManager logger)
    {
        mLogger = logger;
    }

    public Task<object> Run(NameValueCollection queryParams, CancellationToken token)
    {
        var levelString = queryParams.Get("level");
        var provider = queryParams.Get("provider");
        if (string.IsNullOrEmpty(levelString) ||
            string.IsNullOrEmpty(provider) ||
            !Enum.TryParse<LogLevel>(levelString, true, out var logLevel))
            return Task.FromResult<object>(null);

        mLogger.SetMinLogLevel(logLevel, provider);
        mLogger.Info("Cell", $"Changed min log level to {levelString} for {provider} provider");

        return Task.FromResult<object>(null);
    }
}