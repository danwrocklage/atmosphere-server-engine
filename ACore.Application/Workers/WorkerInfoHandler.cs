using System.Collections.Concurrent;
using ACore.Abstractions.Rpc;
using AUtils.Sil;

namespace ACore.Application.Workers;

[Sil(115)]
[Topic(RpcTopics.WORKER, RpcType.Fanout)]
internal class WorkerEvent
{
    public string Name { get; set; }

    public bool IsRunning { get; set; }
}

internal class WorkerInfoHandler : IRpcHandler<WorkerEvent>
{
    internal static readonly ConcurrentDictionary<string, int> GlobalWorkersCount = new();

    public Task Handle(IRpcContext<WorkerEvent> context, CancellationToken token = default)
    {
        GlobalWorkersCount.AddOrUpdate(context.Message.Name, _ => context.Message.IsRunning ? 1 : 0, (_, value) =>
        {
            if (value <= 0)
                return context.Message.IsRunning ? 1 : 0;
            
            return value + (context.Message.IsRunning ? 1 : -1);
        });
        return Task.CompletedTask;
    }
}