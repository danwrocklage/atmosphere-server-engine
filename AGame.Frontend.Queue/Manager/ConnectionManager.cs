using ACore.Abstractions.Rpc;

namespace AGame.Frontend.Queue;

internal class ConnectionManager : IConnectionManager
{
    private struct CellConnections
    {
        private int mCurrentConnections;
        
        public int MaxConnections { get; init; }

        public int CurrentConnections => mCurrentConnections;

        public void UpdateCurrent(int value)
        {
            Interlocked.Add(ref mCurrentConnections, value);
        }
    }

    private readonly Dictionary<Guid, CellConnections> mConnections = new();
    private readonly IRpc mRpc;

    public ConnectionManager(IRpc rpc)
    {
        mRpc = rpc;
    }

    internal void Add(Guid cellId, int connections) => 
        mConnections.TryAdd(cellId, new CellConnections {MaxConnections = connections});

    internal void Remove(Guid cellId) => 
        mConnections.Remove(cellId);

    internal void Update(Guid cellId, bool isConnecting)
    {
        if (mConnections.TryGetValue(cellId, out var fc))
            fc.UpdateCurrent(isConnecting ? 1 : -1);
    }

    public int TotalConnections => 
        mConnections.Values.Sum(x => x.CurrentConnections);

    public async Task<Guid?> ReserveConnection(Guid entityId)
    {
        var availableCell = GetFirstAvailableCellId();
        if (availableCell.HasValue)
            await mRpc.Call($"connection.reserve.{availableCell}", new ConnectionReserveEvent {EntityId = entityId});
        
        return availableCell;
    }
    
    private Guid? GetFirstAvailableCellId()
    {
        if (mConnections.Count < 1)
            return null;

        foreach (var c in mConnections)
        {
            if (c.Value.MaxConnections - c.Value.CurrentConnections > 0)
                return c.Key;
        }
        return null;
    }
}