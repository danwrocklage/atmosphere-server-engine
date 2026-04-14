namespace ACore.Abstractions.Telemetry;

/// <summary>
/// Metric value modification type
/// </summary>
public enum MetricOperationType : byte
{
    /// <summary>
    /// Direct value set
    /// </summary>
    SetValue,
    
    /// <summary>
    /// Increment value
    /// </summary>
    Increment,
    
    /// <summary>
    /// Decrement value
    /// </summary>
    Decrement
}

/// <summary>
/// Common metrics interface
/// </summary>
public interface IMetric
{
    /// <summary>
    /// Update metric value
    /// </summary>
    void Post(int value, MetricOperationType type, params string[] labels);
}

public static class MetricExtensions
{
    public static void Inc(this IMetric metric, int value, params string[] labels)
    {
        metric.Post(value, MetricOperationType.Increment, labels);
    }
        
    public static void Inc(this IMetric metric, params string[] labels)
    {
        metric.Post(1, MetricOperationType.Increment, labels);
    }
        
    public static void Dec(this IMetric metric, int value, params string[] labels)
    {
        metric.Post(value, MetricOperationType.Decrement, labels);
    }
        
    public static void Dec(this IMetric metric, params string[] labels)
    {
        metric.Post(1, MetricOperationType.Decrement, labels);
    }
}