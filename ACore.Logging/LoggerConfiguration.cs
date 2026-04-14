using System.Diagnostics.CodeAnalysis;
using ACore.Abstractions;
using ACore.Abstractions.Logging;

namespace ACore.Logging;

[Configuration("log")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
internal class LoggerConfiguration
{
    internal record LoggerProviderConfiguration(string Type, LogLevel MinLevel, string Url);
    
    public Dictionary<string, LoggerProviderConfiguration> Providers { get; set; }

    public static LoggerConfiguration Default => new()
    {
        Providers = new Dictionary<string, LoggerProviderConfiguration>
        {
            {"console",new LoggerProviderConfiguration("ConsoleProvider", LogLevel.Debug, "")}
        }
    };
}

