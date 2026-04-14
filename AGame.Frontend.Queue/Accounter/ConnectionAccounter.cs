using System.Collections.Concurrent;
using ACore.Abstractions;
using ACore.Abstractions.Rpc;

namespace AGame.Frontend.Queue;

/// <summary>
/// Manager for counting connections
/// </summary>
internal class ConnectionAccounter : IConnectionAccounter
{
    private class ConnectionHolder : IAsyncDisposable
    {
        private readonly ConnectionAccounter mAccounter;

        public ConnectionHolder(ConnectionAccounter accounter)
        {
            mAccounter = accounter;
        }

        public async ValueTask DisposeAsync()
        {
            Interlocked.Decrement(ref mAccounter.mCurrentConnectionsCount);
            await mAccounter.mRpc.Call(new ConnectionStatusEvent
            {
                AppId = Cell.AppId, 
                IsConnecting = false
            });
        }
    }
    
    private readonly IRpc mRpc;
    private readonly Configuration mConfiguration;
    private uint mCurrentConnectionsCount;
    private readonly ConcurrentDictionary<Guid, DateTime> mWaitingConnections;

    public ConnectionAccounter(IConfiguration configuration, IRpc rpc)
    {
        mRpc = rpc;
        mConfiguration = configuration.Get(() => Configuration.Default);
        mCurrentConnectionsCount = 0;
        mWaitingConnections = new ConcurrentDictionary<Guid, DateTime>();
    }

    public bool IsAvailable => 
        mCurrentConnectionsCount < mConfiguration.MaxConnections;

    /// <summary>
    ///     Increment connections count, if it can
    /// </summary>
    /// <returns>Return true, if connections count was incremented, otherwise - false</returns>
    public async Task<IAsyncDisposable> Reserve()
    {
        Interlocked.Increment(ref mCurrentConnectionsCount);
        await mRpc.Call(new ConnectionStatusEvent {AppId = Cell.AppId, IsConnecting = true});
        return new ConnectionHolder(this);
    }

    public bool IsWaiting(Guid entityId) =>
        mWaitingConnections.TryRemove(entityId, out var expiredAt) && 
        expiredAt > DateTime.UtcNow;

    internal void AddWaiter(Guid entityId) => 
        mWaitingConnections.TryAdd(entityId, DateTime.UtcNow + mConfiguration.PrepareTime);

    internal void RemoveExpiredWaiters()
    {
        var now = DateTime.UtcNow;
        foreach (var connection in mWaitingConnections.Keys.ToArray())
        {
            if (mWaitingConnections[connection] <= now)
                mWaitingConnections.TryRemove(connection, out _);
        }
    }
}