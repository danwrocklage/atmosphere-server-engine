using ACore.Abstractions.Logging;
using Sentry;
using Sentry.Extensibility;

namespace ACore.Application;

/// <summary>
/// Adapter for capture logs in sentry
/// </summary>
[Log(Category = "Sentry")]
internal class SentryLoggingAdapter : IDiagnosticLogger
{
    private readonly ILogger<SentryLoggingAdapter> mLogger;

    public SentryLoggingAdapter(ILogger<SentryLoggingAdapter> logger)
    {
        mLogger = logger;
    }

    public bool IsEnabled(SentryLevel level) => true;

    public void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args)
    {
        mLogger.Log(string.Format(message, args), exception, ToCellLogLevel(logLevel));
    }

    private static LogLevel ToCellLogLevel(SentryLevel level) => level switch
    {
        SentryLevel.Debug => LogLevel.Debug,
        SentryLevel.Error => LogLevel.Error,
        SentryLevel.Fatal => LogLevel.Error,
        SentryLevel.Info => LogLevel.Info,
        SentryLevel.Warning => LogLevel.Warning,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };
}