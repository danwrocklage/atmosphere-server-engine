using ACore.Abstractions.Rpc;

namespace ACore.VisualScript;

internal class ScriptChangedHandler : IRpcHandler<ScriptChangedEvent>
{
    public Task Handle(IRpcContext<ScriptChangedEvent> context, CancellationToken token = default)
    {
        ScriptProcessService.CompiledScripts.TryRemove(context.Message.ScriptId, out _);
        return Task.CompletedTask;
    }
}