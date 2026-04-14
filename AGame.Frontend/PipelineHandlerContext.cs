using AGame.Frontend.Dto;

namespace AGame.Frontend;

/// <summary>
/// Client's message processing context
/// </summary>
public sealed class PipelineHandlerContext
{
    internal PipelineHandlerContext() { }
    
    /// <summary>
    /// Client linked entity id
    /// </summary>
    public Guid EntityId { get; internal init; }
    
    /// <summary>
    /// Connection cancellation token
    /// </summary>
    public CancellationToken CancellationToken { get; internal init; }

    /// <summary>
    /// Ends communication with client and close connection
    /// </summary>
    public object Close() => CloseConnectionDto.Instance;

    public event Func<Task> OnClose;

    internal Task OnCloseInvoke() => OnClose?.Invoke() ?? Task.CompletedTask;
}