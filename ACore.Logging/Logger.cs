using System.Diagnostics;
using ACore.Abstractions;
using ACore.Abstractions.Extensions;
using ACore.Abstractions.Logging;
using ACore.Application;
using AUtils.IoC;
using BindingFlags = System.Reflection.BindingFlags;

namespace ACore.Logging;

internal class Logger : ILoggerManager
{    
    internal readonly Dictionary<string, ILoggerProvider> Providers;

    public Logger(IConfiguration configuration, IContainer container)
    {
        var config = configuration.Get(() => LoggerConfiguration.Default);

        var providerTypes = Types.All
            .Where(x => x.GetInterface(nameof(ILoggerProvider)) != null)
            .ToDictionary(x => x.Name, x => x);

        Providers = new Dictionary<string, ILoggerProvider>();
        foreach (var providerConfiguration in config.Providers)
        {
            if (!providerTypes.TryGetValue(providerConfiguration.Value.Type, out var providerType))
            {
                DebugLogger.WriteLine($"Logger provider '{providerConfiguration.Value.Type}' was not found",
                    ConsoleColor.Yellow);
                continue;
            }

            var constructor = providerType.GetConstructor(BindingFlags.Instance | BindingFlags.Public,
                new[] {typeof(Uri), typeof(IContainer)});
            
            Uri url = null;
            if (!string.IsNullOrEmpty(providerConfiguration.Value.Url))
            {
                url = new Uri(providerConfiguration.Value.Url, UriKind.RelativeOrAbsolute);
                if (!url.IsAbsoluteUri)
                    url = new Uri(new Uri(Environment.CurrentDirectory + Path.DirectorySeparatorChar), url);
            }

            var provider = (ILoggerProvider) (constructor?.Invoke(new object[] {url, container}) ??
                                              Activator.CreateInstance(providerType));

            if (provider == null)
            {
                DebugLogger.WriteLine($"Logger provider '{providerConfiguration.Value.Type}' was not created",
                    ConsoleColor.Yellow);
                continue;
            }

            provider.MinLogLevel = providerConfiguration.Value.MinLevel;
            Providers.Add(providerConfiguration.Key, provider);
            DebugLogger.WriteLine($"Logger provider '{providerConfiguration.Value.Type}' was added",
                ConsoleColor.DarkGray);
        }
    }

    public void Log(string message)
    {
        Trace.WriteLine(message, nameof(Logger));
        foreach (var provider in Providers)
        {
            _ = provider.Value.Write(message)
                .ContinueWith(t => DebugLogger.WriteLine($"Log was failed: {t.Exception.GetFullMessage()}", ConsoleColor.Red), TaskContinuationOptions.OnlyOnFaulted)
                .ConfigureAwait(false);
        }
    }

    public void Log(string section, string message, LogLevel level = LogLevel.Info) => 
        Log(section, message, null, level);

    public void Log(string section, string message, Exception ex, LogLevel level = LogLevel.Error)
    {
        var messageDto = new Message(message, section, level, DateTime.Now, ex, Environment.CurrentManagedThreadId);
        Trace.WriteLine(message, nameof(Logger));

        foreach (var provider in Providers)
        {
            _ = provider.Value.Write(messageDto)
                .ContinueWith(t => DebugLogger.WriteLine($"Log was failed: {t.Exception.GetFullMessage()}", ConsoleColor.Red), TaskContinuationOptions.OnlyOnFaulted)
                .ConfigureAwait(false);
        }
    }
    
    public void AddProvider<T>(T provider, string name) where T : class, ILoggerProvider
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (Providers.ContainsKey(name) || Providers.ContainsValue(provider))
            throw new ArgumentException($"{nameof(ILoggerProvider)} or {nameof(name)} is already used");
            
        Providers.Add(name, provider);
        
        DebugLogger.WriteLine($"A new {nameof(ILoggerProvider)} with name '{name}' was added ({provider.GetType().FullName})");
    }

    public void SetMinLogLevel(LogLevel minLogLevel, string name)
    {
        if (!Providers.TryGetValue(name, out var provider)) 
            return;
        
        provider.MinLogLevel = minLogLevel;
        DebugLogger.WriteLine($"A {nameof(ILoggerProvider.MinLogLevel)} was changed for {nameof(ILoggerProvider)} with '{name}' ({minLogLevel})");
    }
}