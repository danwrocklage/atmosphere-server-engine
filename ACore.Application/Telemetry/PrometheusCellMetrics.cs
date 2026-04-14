using System.Collections.Concurrent;
using ACore.Abstractions;
using ACore.Abstractions.Telemetry;
using Prometheus.Client;
using IMetric = ACore.Abstractions.Telemetry.IMetric;

namespace ACore.Application.Telemetry;

/// <inheritdoc />
internal class PrometheusCellMetrics : ICellMetrics
{
    private readonly string mMetricsPrefixName;
    private readonly ConcurrentDictionary<string, IMetric> mMetrics = new();

    public PrometheusCellMetrics(IConfiguration configuration)
    {
        var config = configuration.Get(() => MetricsConfiguration.Default);
        mMetricsPrefixName = config.Prefix;
    }
    
    /// <inheritdoc />
    public void Create(MetricDescription description)
    {
        if (!description.IsValid())
            throw new ArgumentException($"{nameof(description)} isn't valid", nameof(description));
        
        if(mMetrics.ContainsKey(description.Name))
        {
            if(GetType(mMetrics[description.Name]) != description.Type)
                throw new ArgumentException("Metric already exists", nameof(description));
            return;
        }

        IMetric result = description.Type switch
        {
            MetricsType.Counter => new CounterMetric(Metrics.DefaultFactory
                .CreateCounterInt64($"{mMetricsPrefixName}_{description.Name}", description.Description, false, description.Labels)),
            MetricsType.Gauge => new GaugeMetric(Metrics.DefaultFactory
                .CreateGaugeInt64($"{mMetricsPrefixName}_{description.Name}", description.Description, false, description.Labels)),
            MetricsType.Summary => new SummaryMetric(Metrics.DefaultFactory
                .CreateSummary($"{mMetricsPrefixName}_{description.Name}", description.Description, false, description.Labels)),
            MetricsType.Histogram => new HistogramMetric(Metrics.DefaultFactory
                .CreateHistogram($"{mMetricsPrefixName}_{description.Name}", description.Description, false, description.Labels)),
            _ => throw new ArgumentOutOfRangeException()
        };

        mMetrics.TryAdd(description.Name, result);
    }

    /// <inheritdoc />
    public IMetric Get(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        
        return mMetrics.TryGetValue(name, out var metric) ? metric : null;
    }

    private static MetricsType GetType(IMetric metric) =>
        metric switch
        {
            CounterMetric => MetricsType.Counter,
            GaugeMetric => MetricsType.Gauge,
            SummaryMetric => MetricsType.Summary,
            HistogramMetric => MetricsType.Histogram,
            _ => throw new ArgumentOutOfRangeException(nameof(metric), "Unknown metric type")
        };

    #region Metric Wrappers

    private class CounterMetric : IMetric
    {
        private readonly IMetricFamily<ICounter<long>> mCounter;

        public CounterMetric(IMetricFamily<ICounter<long>> counter)
        {
            mCounter = counter;
        }

        /// <inheritdoc />
        public void Post(int value, MetricOperationType type, params string[] labels)
        {
            if (type != MetricOperationType.Increment)
                throw new NotSupportedException();
            
            mCounter.WithLabels(labels).IncTo(value);
        }
    }
    
    private class GaugeMetric : IMetric
    {
        private readonly IMetricFamily<IGauge<long>> mGauge;

        public GaugeMetric(IMetricFamily<IGauge<long>> gauge)
        {
            mGauge = gauge;
        }

        /// <inheritdoc />
        public void Post(int value, MetricOperationType type, params string[] labels)
        {
            if (type == MetricOperationType.Increment)
                mGauge.WithLabels(labels).IncTo(value);
            
            if (type == MetricOperationType.Decrement)
                mGauge.WithLabels(labels).DecTo(value);
            
            if (type == MetricOperationType.SetValue)
                mGauge.WithLabels(labels).Set(value);
        }
    }
    
    private class SummaryMetric : IMetric
    {
        private readonly IMetricFamily<ISummary> mSummary;

        public SummaryMetric(IMetricFamily<ISummary> summary)
        {
            mSummary = summary;
        }

        /// <inheritdoc />
        public void Post(int value, MetricOperationType type, params string[] labels)
        {
            if (type != MetricOperationType.SetValue)
                throw new NotSupportedException();
            
            mSummary.WithLabels(labels).Observe(value);
        }
    }
    
    private class HistogramMetric : IMetric
    {
        private readonly IMetricFamily<IHistogram> mHistogram;

        public HistogramMetric(IMetricFamily<IHistogram> histogram)
        {
            mHistogram = histogram;
        }

        /// <inheritdoc />
        public void Post(int value, MetricOperationType type, params string[] labels)
        {
            if (type != MetricOperationType.SetValue)
                throw new NotSupportedException();
            
            mHistogram.WithLabels(labels).Observe(value);
        }
    }

    #endregion

    #region Utils

    [Configuration("metrics")]
    private class MetricsConfiguration
    {
        public string Prefix { get; set; }

        public static MetricsConfiguration Default => new()
        {
            Prefix = "acore"
        };
    }

    #endregion
}