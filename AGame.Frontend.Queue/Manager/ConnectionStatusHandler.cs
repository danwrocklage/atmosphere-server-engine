using ACore.Abstractions.Rpc;

namespace AGame.Frontend.Queue;

internal class ConnectionStatusHandler : IRpcHandler<ConnectionStatusEvent>
{
    private readonly ConnectionManager mConnectionManager;

    public ConnectionStatusHandler(ConnectionManager connectionManager)
    {
        mConnectionManager = connectionManager;
    }

    public Task Handle(IRpcContext<ConnectionStatusEvent> context, CancellationToken token = default)
    {
        mConnectionManager.Update(context.Message.AppId, context.Message.IsConnecting);
        return Task.CompletedTask;
    }
}