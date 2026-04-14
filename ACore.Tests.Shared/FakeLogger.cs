using ACore.Abstractions.Logging;

namespace ACore.Tests.Shared;

internal class FakeLogger : ILogger
{
    public void Log(string message) { }

    public void Log(string section, string message, LogLevel level) { }

    public void Log(string section, string message, Exception ex, LogLevel level) { }
}