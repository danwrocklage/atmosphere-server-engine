namespace ACore.Abstractions.Telemetry;

/// <summary>
/// Model for creating new metrics
/// </summary>
public readonly record struct MetricDescription(string Name, MetricsType Type, string Description, string[] Labels)
{
    /// <summary>
    /// Is model valid?
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Name);
    }
}
    
/// <summary>
/// Common interface for managing metrics
/// </summary>
public interface ICellMetrics
{
    /// <summary>
    /// Create new metric.
    /// If metric with <see cref="MetricDescription.Name"/> already exists, it will be returned 
    /// </summary>
    void Create(MetricDescription description);

    /// <summary>
    /// Create new metric.
    /// If metric with <see cref="MetricDescription.Name"/> already exists, nothing will do
    /// </summary>
    void Create(string name, MetricsType type, string description = null, params string[] labels) =>
        Create(new MetricDescription(name, type, description, labels));

    /// <summary>
    /// Get existing metric by name
    /// </summary>
    IMetric Get(string name);
}