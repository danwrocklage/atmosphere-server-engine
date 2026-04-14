using ACore.Abstractions.Rpc;

namespace ACore.Application.Cluster;

/// <summary>
/// Handler for ping message
/// </summary>
internal class PingHandler : IRpcHandler<PingMessage>
{
    private readonly CellCluster mCellCluster;

    public PingHandler(CellCluster cellCluster)
    {
        mCellCluster = cellCluster;
    }

    public Task Handle(IRpcContext<PingMessage> context, CancellationToken token = default) => 
        mCellCluster.ProcessCellPing(context.Message.Id, token);
}