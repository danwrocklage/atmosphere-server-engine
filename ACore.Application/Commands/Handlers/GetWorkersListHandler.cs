using System.Collections.Specialized;
using System.ComponentModel;
using ACore.Abstractions;
using ACore.Abstractions.Worker;
using ACore.Application.Workers;

namespace ACore.Application.Commands.Handlers;

[DisplayName("worker.list")]
internal class GetWorkersListHandler : ICommandHandler
{
    private readonly ICellWorkers mCellWorkers;

    public GetWorkersListHandler(ICellWorkers cellWorkers)
    {
        mCellWorkers = cellWorkers;
    }

    public Task<object> Run(NameValueCollection queryParams, CancellationToken token)
    {
        var runningWorkers = mCellWorkers.GetRunningWorkers();
        var idleWorkers = CellWorkersService.WorkerNames
            .Where(x => runningWorkers.All(r => r.Worker != x))
            .Select(x =>
                new RunningWorker
                {
                    Name = null,
                    Worker = x,
                    Type = CellWorkersService.WorkerTypes[WorkerType.Regular]
                })
            .Concat(runningWorkers)
            .ToArray();
        return Task.FromResult((object) idleWorkers);
    }
}