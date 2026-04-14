using ACore.Abstractions;
using ACore.Abstractions.Worker;

namespace AGame.Frontend.Queue;

[Worker("remove-expired-reservations")]
internal class RemoveExpiredReservationsWorker : IRunnable
{
    private readonly ConnectionAccounter mConnectionAccounter;

    public RemoveExpiredReservationsWorker(ConnectionAccounter connectionAccounter)
    {
        mConnectionAccounter = connectionAccounter;
    }

    public async Task Run(CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(1000);
            mConnectionAccounter.RemoveExpiredWaiters();
        }
    }
}