using System.Collections.Concurrent;
using ACore.Abstractions.Telemetry;

namespace Fb.Frontend.Bot;

internal class BotMetrics : ICellMetrics
{
    private readonly ConcurrentDictionary<string, IMetric> mMetrics = new();

    public void Create(MetricDescription description)
    {
        mMetrics.TryAdd(description.Name, new BotMetric());
    }

    public IMetric Get(string name) => mMetrics[name];

    private class BotMetric : IMetric
    {
        private int mValue = 0;
        
        public void Post(int value, MetricOperationType type, params string[] labels)
        {
            switch (type)
            {
                case MetricOperationType.SetValue:
                    mValue = value;
                    break;
                case MetricOperationType.Increment:
                    mValue += value;
                    break;
                case MetricOperationType.Decrement:
                    mValue -= value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}