using ACore.Abstractions.Telemetry;

namespace ACore.Tests.Shared;

internal class FakeMetrics : ICellMetrics
{
    private readonly FakeMetric mMetric = new();
    
    public void Create(MetricDescription description) {}

    public IMetric Get(string name) => mMetric;
    
    private class FakeMetric : IMetric
    {
        public void Post(int value, MetricOperationType type, params string[] labels) { }
    }
}