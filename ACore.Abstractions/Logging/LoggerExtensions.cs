namespace ACore.Abstractions.Logging;

public static class LoggerExtensions
{
    public static void Debug(this ILogger logger, string section, string message) => logger.Log(section, message, LogLevel.Debug);
    public static void Debug<T>(this ILogger<T> logger, string message) => logger.Log(message, LogLevel.Debug);
    
    public static void Debug(this ILogger logger, string section, string message, Exception exception) => logger.Log(section, message, exception, LogLevel.Debug);
    public static void Debug<T>(this ILogger<T> logger, string message, Exception exception) => logger.Log(message, exception, LogLevel.Debug);
    
    public static void Info(this ILogger logger, string section, string message) => logger.Log(section, message, LogLevel.Info);
    public static void Info<T>(this ILogger<T> logger, string message) => logger.Log(message, LogLevel.Info);
    
    public static void Info(this ILogger logger, string section, string message, Exception exception) => logger.Log(section, message, exception, LogLevel.Info);
    public static void Info<T>(this ILogger<T> logger, string message, Exception exception) => logger.Log(message, exception, LogLevel.Info);
    
    public static void Success(this ILogger logger, string section, string message) => logger.Log(section, message, LogLevel.Success);
    public static void Success<T>(this ILogger<T> logger, string message) => logger.Log(message, LogLevel.Success);
    
    public static void Warn(this ILogger logger, string section, string message) => logger.Log(section, message, LogLevel.Warning);
    public static void Warn<T>(this ILogger<T> logger, string message) => logger.Log(message, LogLevel.Warning);
    
    public static void Warn(this ILogger logger, string section, string message, Exception exception) => logger.Log(section, message, exception, LogLevel.Warning);
    public static void Warn<T>(this ILogger<T> logger, string message, Exception exception) => logger.Log(message, exception, LogLevel.Warning);
    
    public static void Error(this ILogger logger, string section, string message) => logger.Log(section, message, LogLevel.Error);
    public static void Error<T>(this ILogger<T> logger, string message) => logger.Log(message, LogLevel.Error);
    
    public static void Error(this ILogger logger, string section, string message, Exception exception) => logger.Log(section, message, exception, LogLevel.Error);
    public static void Error<T>(this ILogger<T> logger, string message, Exception exception) => logger.Log(message, exception, LogLevel.Error);
    
    public static void Fatal(this ILogger logger, string section, string message) => logger.Log(section, message, LogLevel.Fatal);
    public static void Fatal<T>(this ILogger<T> logger, string message) => logger.Log(message, LogLevel.Fatal);
    
    public static void Fatal(this ILogger logger, string section, string message, Exception exception) => logger.Log(section, message, exception, LogLevel.Fatal);
    public static void Fatal<T>(this ILogger<T> logger, string message, Exception exception) => logger.Log(message, exception, LogLevel.Fatal);
}