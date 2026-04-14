using ACore.Abstractions;
using ACore.Abstractions.Worker;

namespace ACore.Application.Cluster;

/// <summary>
/// Worker for track information about cells (new & disconnected)
/// </summary>
[Worker("cell-watch")]
internal class CellWatchWorker : IRunnable
{
    private readonly CellCluster mCellCluster;

    public CellWatchWorker(CellCluster cellCluster)
    {
        mCellCluster = cellCluster;
    }

    public async Task Run(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await mCellCluster.UpdateCells();
            await Task.Delay(1000, token);
        }
    }
}