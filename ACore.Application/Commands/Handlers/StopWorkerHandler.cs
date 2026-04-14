using System.Collections.Specialized;
using System.ComponentModel;
using ACore.Abstractions;
using ACore.Abstractions.Worker;

namespace ACore.Application.Commands.Handlers;

[DisplayName("worker.stop")]
internal class StopWorkerHandler : ICommandHandler
{
    private readonly ICellWorkers mCellWorkers;

    public StopWorkerHandler(ICellWorkers cellWorkers)
    {
        mCellWorkers = cellWorkers;
    }

    public Task<object> Run(NameValueCollection queryParams, CancellationToken token)
    {
        if (!queryParams.AllKeys.Contains("name"))
            return Task.FromResult((object) false);
            
        var name = queryParams["name"];
            
        if(string.IsNullOrEmpty(name))
            return Task.FromResult((object) false);

        mCellWorkers.Stop(name);
            
        return Task.FromResult((object) true);
    }
}