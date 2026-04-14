using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Rpc;

namespace ACore.Application.Cluster;


/// <inheritdoc />
[Log(Category = "cluster")]
internal class CellCluster : ICellCluster
{
    private readonly TimeSpan mInterval;
    private readonly ConcurrentDictionary<Guid, DateTime> mCellLastPing;
    private readonly ConcurrentDictionary<Guid, CellInfo> mCellInfos;
    private readonly ILogger<CellCluster> mLogger;
    private readonly IRpc mRpc;

    public CellCluster(ILogger<CellCluster> logger, IRpc rpc)
    {
        mLogger = logger;
        mRpc = rpc;
        mInterval = Debugger.IsAttached ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(5);
        mCellInfos = new ConcurrentDictionary<Guid, CellInfo>();
        mCellLastPing = new ConcurrentDictionary<Guid, DateTime>();
    }

    private async Task AddCell(Guid id, CancellationToken token)
    {
        mCellLastPing.TryAdd(id, DateTime.UtcNow);
        var info = await mRpc.Call<CellInfoRequest, CellInfo>($"{RpcTopics.PING}.{id}", new CellInfoRequest(), token);
        mCellInfos.TryAdd(id, info);

        if (id == CellPingWorker.PingMessage.Id)
            return;

        mLogger.Debug($"New cell found ({info.Role} {id})");
        if(CellFound != null)
            await CellFound(info);
    }

    /// <summary>
    /// Update ping timestamp for existing cell. If cell is new - add it
    /// </summary>
    internal async Task ProcessCellPing(Guid id, CancellationToken token = default)
    {
        if (!mCellLastPing.ContainsKey(id))
        {
            await AddCell(id, token);
            return;
        }

        mCellLastPing[id] = DateTime.UtcNow;
    }

    /// <summary>
    /// Check ping timestamps and remove cell if it's outdated
    /// </summary>
    internal async Task UpdateCells()
    {
        var now = DateTime.UtcNow;
        var ids = mCellLastPing.Keys.ToArray();
        foreach (var id in ids)
        {
            if (!mCellLastPing.TryGetValue(id, out var ping) || 
                now - ping <= mInterval)
                continue;

            mCellLastPing.TryRemove(id, out _);
            
            if(!mCellInfos.TryRemove(id, out var cellInfo))
                continue;
            mLogger.Warn($"Cell was lost ({cellInfo} {id})");
            if(CellLost != null && cellInfo != null)
                await CellLost(cellInfo);
        }
    }

    internal Task OnCellError(CellError cellError)
    {
        mLogger.Debug($"Cell error received: {cellError.AppId}");
        return CellError == null ? Task.CompletedTask : CellError(cellError);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<CellInfo> Cells => (ReadOnlyCollection<CellInfo>) mCellInfos.Values;

    /// <inheritdoc />
    public string GetRoleEndpoint(Guid cellId) =>
        mCellInfos.Values.FirstOrDefault(x => x.AppId == cellId)?.Endpoint;

    /// <inheritdoc />
    public bool IsRoleAvailable(string cellRole) =>
        mCellInfos.Values.Any(x => x.Role == cellRole);

    /// <inheritdoc />
    public bool IsCellIdExists(Guid cellId) => mCellInfos.Values.Any(x => x.AppId == cellId);

    /// <inheritdoc />
    public event ICellCluster.CellClusterModified CellFound;

    /// <inheritdoc />
    public event ICellCluster.CellClusterModified CellLost;

    /// <inheritdoc />
    public event ICellCluster.CellErrorReceived CellError;
}