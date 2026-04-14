namespace ACore.VisualScript;

public interface IScriptProcessService
{
    Task Compile(Guid scriptId, CancellationToken token = default);

    Task Execute(Guid scriptId, object?[]? args = null, CancellationToken token = default);
}