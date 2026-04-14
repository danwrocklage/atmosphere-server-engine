using ACore.Abstractions.Rpc;

namespace AGame.Frontend.Queue;

internal class ConnectionReserveHandler : IRpcHandler<ConnectionReserveEvent>
{
    private readonly ConnectionAccounter mConnectionAccounter;

    public ConnectionReserveHandler(ConnectionAccounter connectionAccounter)
    {
        mConnectionAccounter = connectionAccounter;
    }

    public Task Handle(IRpcContext<ConnectionReserveEvent> context, CancellationToken token = default)
    {
        mConnectionAccounter.AddWaiter(context.Message.EntityId);
        return Task.CompletedTask;
    }
}