namespace ACore.Abstractions.Logging;

/// <summary>
/// A full log message
/// </summary>
/// <param name="Text">Message body</param>
/// <param name="Section">Message section</param>
/// <param name="Level">Log level for message</param>
/// <param name="Time">Current timestamp</param>
/// <param name="Exception">Exception (if exists)</param>
/// <param name="ThreadId">Current thread ID</param>
public readonly record struct Message(string Text, string Section, LogLevel Level, DateTime Time, Exception Exception = null, int? ThreadId = null);

/// <summary>
/// Interface for log writing destination
/// </summary>
public interface ILoggerProvider
{
    /// <summary>
    /// Get or set minimum level for log in this provider
    /// </summary>
    LogLevel MinLogLevel { get; set; }
        
    /// <summary>
    /// Write a full message
    /// </summary>
    Task Write(Message message);
        
    /// <summary>
    /// Write a simple message
    /// </summary>
    Task Write(string message);
}