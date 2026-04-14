using ACore.Abstractions;
using ACore.Abstractions.Rpc;

namespace ACore.Application.Cluster;

internal class CellErrorHandler : IRpcHandler<CellError>
{
    private readonly CellCluster mCellCluster;

    public CellErrorHandler(CellCluster cellCluster)
    {
        mCellCluster = cellCluster;
    }

    public Task Handle(IRpcContext<CellError> context, CancellationToken token = default)
    {
        return mCellCluster.OnCellError(context.Message);
    }
}