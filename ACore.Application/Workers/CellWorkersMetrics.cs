using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Telemetry;
using ACore.Abstractions.Worker;

namespace ACore.Application.Workers;

[Log(Category = "Cell")]
internal class CellWorkersMetrics : IInitializable
{
    private readonly ICellMetrics mMetrics;
    private readonly ICellWorkers mWorkers;
    private readonly ILogger<CellWorkersMetrics> mLogger;
    private readonly string mRole;

    public CellWorkersMetrics(ICellEnvironment info, ICellMetrics metrics, ICellWorkers workers, ILogger<CellWorkersMetrics> logger)
    {
        mMetrics = metrics;
        mWorkers = workers;
        mLogger = logger;
        mRole = info.Role;
    }

    public void Initialize()
    {
        mLogger.Debug("Add cell workers metrics");
        
        mMetrics.Create("cell_workers_running_count", MetricsType.Gauge, "Workers", "role", "name");
        mMetrics.Create("cell_workers_errors_count", MetricsType.Counter, "Workers", "role", "name");

        mWorkers.OnStart += name => { mMetrics.Get("cell_workers_running_count").Inc(mRole, name); return Task.CompletedTask; };
        mWorkers.OnStop += name => { mMetrics.Get("cell_workers_running_count").Dec(mRole, name); return Task.CompletedTask; };
        mWorkers.OnError += (name, _) => { mMetrics.Get("cell_workers_errors_count").Inc(mRole, name); return Task.CompletedTask; };
    }
}