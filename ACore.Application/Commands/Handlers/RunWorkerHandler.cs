using System.Collections.Specialized;
using System.ComponentModel;
using ACore.Abstractions;
using ACore.Abstractions.Worker;

namespace ACore.Application.Commands.Handlers;

[DisplayName("worker.run")]
internal class RunWorkerHandler : ICommandHandler
{
    private readonly ICellWorkers mCellWorkers;

    public RunWorkerHandler(ICellWorkers cellWorkers)
    {
        mCellWorkers = cellWorkers;
    }

    public Task<object> Run(NameValueCollection queryParams, CancellationToken token)
    {
        var name = queryParams.Get("name");
        var type = queryParams.Get("type");
        var cron = queryParams.Get("cron");
            
        if(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
            return Task.FromResult((object) new { IsSuccess = false });

        if(!string.IsNullOrEmpty(cron))
            mCellWorkers.Run(name, type, token, cron);
        else
            mCellWorkers.Run(name, type, token, 2);
            
        return Task.FromResult((object) new { IsSuccess = true });
    }
}