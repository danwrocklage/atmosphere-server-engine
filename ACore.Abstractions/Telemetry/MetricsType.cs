namespace ACore.Abstractions.Telemetry;

/// <summary>
/// Types of metrics
/// </summary>
public enum MetricsType : byte
{
    /// <summary>
    /// Simple forward counter
    /// </summary>
    Counter,
    /// <summary>
    /// Bidirectional counter
    /// </summary>
    Gauge,
    
    /// <summary>
    /// Bucket separated counter with total
    /// </summary>
    Summary,
    
    /// <summary>
    /// Bucket separated counter
    /// </summary>
    Histogram
}