namespace AGame.Frontend;

internal interface IPipelineHandler
{
    /// <summary>
    /// Process client message and return result to send back
    /// </summary>
    Task<object> Handle(object message, PipelineHandlerContext context);
}

/// <summary>
/// Client message/command processor
/// </summary>
public abstract class PipelineHandler<T> : IPipelineHandler
{
    /// <summary>
    /// Process client message and return result to send back
    /// </summary>
    protected abstract Task<object> Handle(T message, PipelineHandlerContext context);

    Task<object> IPipelineHandler.Handle(object message, PipelineHandlerContext context) =>
        Handle((T) message, context);
}