using System.Collections.Concurrent;
using ACore.Abstractions.Logging;
using ACore.Logging.Providers;
using AUtils.IoC;

namespace ACore.Logging;

public class LoggingModule : ACore.Modules.Module
{
    internal static readonly ConcurrentDictionary<Type, object> TypedLoggersCache = new();

    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Register(x => x.For<Logger>().As<ILogger>().As<ILoggerManager>().AsSelf().Singleton());
        builder.Register(x => x.For(typeof(TypedLogger<>), (c, t) =>
        {
            if (!TypedLoggersCache.ContainsKey(t))
                TypedLoggersCache.TryAdd(t, Activator.CreateInstance(t, c.Resolve<ILogger>()));
            return TypedLoggersCache[t];
        }).As(typeof(ILogger<>)));

        builder.OnBuilt += c =>
        {
            if (c.Resolve<Logger>().Providers.Values.Any(x => x is ConsoleProvider))
                ConsoleProvider.ShowHeader();
        };
    }
}