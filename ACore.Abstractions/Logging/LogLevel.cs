namespace ACore.Abstractions.Logging;

/// <summary>
/// Logger log level
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Lowest level. Development only
    /// </summary>
    Debug,
        
    /// <summary>
    /// Standard information level
    /// </summary>
    Info,
        
    /// <summary>
    /// Standard information level for good done job
    /// </summary>
    Success,
        
    /// <summary>
    /// Pay attention
    /// </summary>
    Warning,
        
    /// <summary>
    /// Something gets wrong
    /// </summary>
    Error,
        
    /// <summary>
    /// Application crash
    /// </summary>
    Fatal
}