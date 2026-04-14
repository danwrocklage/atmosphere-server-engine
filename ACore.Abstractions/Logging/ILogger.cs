namespace ACore.Abstractions.Logging;

/// <summary>
/// Simple logger
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Log simple message (supported only by console provider)
    /// </summary>
    void Log(string message);
        
    /// <summary>
    /// Write a full message
    /// </summary>
    void Log(string section, string message, LogLevel level);
        
    /// <summary>
    /// Write a full message with exception
    /// </summary>
    void Log(string section, string message, Exception ex, LogLevel level);
}

/// <summary>
/// Logger with section as a type name
/// </summary>
public interface ILogger<T>
{
    /// <summary>
    /// Write a full message
    /// </summary>
    void Log(string message, LogLevel level);
        
    /// <summary>
    /// Write a full message with exception
    /// </summary>
    void Log(string message, Exception ex, LogLevel level);

    /// <summary>
    /// Create new logger from current
    /// </summary>
    ILogger<TSub> ToLogger<TSub>();

    /// <summary>
    /// Get base logger
    /// </summary>
    ILogger ToLogger();
}
    
/// <summary>
/// Manage logging
/// </summary>
public interface ILoggerManager : ILogger
{
    /// <summary>
    /// Add a new named <see cref="ILoggerProvider"/> at runtime
    /// </summary>
    void AddProvider<T>(T provider, string name) where T : class, ILoggerProvider;

    /// <summary>
    /// Apply new minimum log level for <see cref="ILoggerProvider"/>
    /// </summary>
    void SetMinLogLevel(LogLevel minLogLevel, string name);
}