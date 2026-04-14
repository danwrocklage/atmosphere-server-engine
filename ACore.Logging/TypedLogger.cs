using System.Reflection;
using ACore.Abstractions.Logging;

namespace ACore.Logging;

/// <inheritdoc cref="ILogger{T}"/>
internal class TypedLogger<T> : ILogger<T>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly string sSection;
    
    static TypedLogger()
    {
        var type = typeof(T);
        type = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        var attr = type.GetCustomAttribute<LogAttribute>();
        sSection = attr != null && !string.IsNullOrEmpty(attr.Category) ? attr.Category : type.Name;
    }

    private readonly ILogger mLogger;

    // ReSharper disable once MemberCanBePrivate.Global
    public TypedLogger(ILogger logger)
    {
        mLogger = logger;
    }

    /// <inheritdoc />
    public void Log(string message, LogLevel level) => mLogger.Log(sSection, message, level);

    /// <inheritdoc />
    public void Log(string message, Exception ex, LogLevel level) => mLogger.Log(sSection, message, ex, level);

    /// <inheritdoc />
    public ILogger<TSub> ToLogger<TSub>()
    {
        var t = typeof(TypedLogger<TSub>);
        if (!LoggingModule.TypedLoggersCache.ContainsKey(t))
            LoggingModule.TypedLoggersCache.TryAdd(t, new TypedLogger<TSub>(mLogger));
        return (TypedLogger<TSub>) LoggingModule.TypedLoggersCache[t];
    }

    /// <inheritdoc />
    public ILogger ToLogger() => mLogger;
}