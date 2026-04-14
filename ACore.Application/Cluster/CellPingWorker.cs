using ACore.Abstractions;
using ACore.Abstractions.Rpc;
using ACore.Abstractions.Worker;

namespace ACore.Application.Cluster;

/// <summary>
/// Worker for sending ping message to other cells
/// </summary>
[Worker("cell-ping")]
internal class CellPingWorker : IRunnable
{
    private readonly IRpc mRpc;
    
    internal static PingMessage PingMessage { get; } = new() { Id = Cell.AppId };

    public CellPingWorker(IRpc rpc, IRpcSubscribe subscriber)
    {
        mRpc = rpc;
        subscriber?.Subscribe<PingMessage>();
        subscriber?.Subscribe<CellError>();
        subscriber?.Subscribe<CellInfoRequest>($"{RpcTopics.PING}.{PingMessage.Id}");
    }

    public async Task Run(CancellationToken token)
    {
        if(mRpc == null)
            return;
        
        while (!token.IsCancellationRequested)
        {
            await mRpc.Call(PingMessage, token);
            await Task.Delay(2000, token);
        }
    }
}