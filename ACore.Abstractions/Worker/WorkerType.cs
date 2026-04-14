namespace ACore.Abstractions.Worker;

/// <summary>
/// Type of cell worker
/// </summary>
public enum WorkerType : byte
{
    /// <summary>
    /// One time run worker
    /// </summary>
    Regular,
    
    /// <summary>
    /// Scheduled worker
    /// </summary>
    Cron
}