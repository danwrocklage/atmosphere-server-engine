using ACore.Abstractions.Logging;

namespace ACore.Tests.Shared;

internal class TypedFakeLogger<T> : ILogger<T>
{
    public void Log(string message, LogLevel level) { }

    public void Log(string message, Exception ex, LogLevel level) { }

    public ILogger<TSub> ToLogger<TSub>() => new TypedFakeLogger<TSub>();

    public ILogger ToLogger() => new FakeLogger();
}