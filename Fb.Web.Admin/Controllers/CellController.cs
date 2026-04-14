using System.ComponentModel;
using System.Net.Http.Json;
using ACore.Abstractions;
using ACore.Abstractions.Worker;
using ACore.Worker.Web.Routing;
using ACore.Worker.Web.Routing.Attributes;

namespace Fb.Web.Admin.Controllers;

public class CellController : BaseAdminController
{
    private readonly ICellCluster mCluster;

    public CellController(ICellCluster cluster)
    {
        mCluster = cluster;
    }

    [Get]
    [Description("Get all cells basic info")]
    public async Task GetCells()
    {
        await Response(mCluster.Cells
            .Select(x => new
            {
                x.AppId,
                x.Role,
                x.Configuration,
                x.Build
            })
            .ToArray());
    }

    [Get("summary")]
    [Description("Get summary info about running cells")]
    public async Task GetSummaryCells()
    {
        var groups = mCluster.Cells
            .GroupBy(x => x.Role)
            .ToDictionary(x => x.Key, x => x.Count());

        await Response(groups);
    }

    [Get("{id}")]
    [Description("Get summary info about running cells")]
    public async Task GetCellDetails([FromRoute] string id)
    {
        if (!Guid.TryParse(id, out var appId))
        {
            Response(400);
            return;
        }

        var info = mCluster.Cells.FirstOrDefault(x => x.AppId == appId);
        if (info == null)
        {
            Response(404);
            return;
        }

        using var http = new HttpClient();
        http.BaseAddress = new Uri(info.Endpoint);
        var workers = await http.GetFromJsonAsync<RunningWorker[]>("/ctrl/worker.list");

        await Response(new
        {
            info.Configuration,
            info.Build,
            info.Role,
            Workers = workers
        });
    }
}