namespace ACore.Worker.Web.Routing;

/// <summary>
/// Endpoint method parameter type
/// </summary>
internal enum ActionParameterType
{
    /// <summary>
    /// JSON request body
    /// </summary>
    Body,
    
    /// <summary>
    /// Query parameter
    /// </summary>
    Query,
    
    /// <summary>
    /// Route parameter
    /// </summary>
    Route,
    
    /// <summary>
    /// DI parameter
    /// </summary>
    Service,
    
    /// <summary>
    /// Header parameter
    /// </summary>
    Header,
    
    /// <summary>
    /// Body as stream
    /// </summary>
    Stream,
    
    /// <summary>
    /// Cancellation token
    /// </summary>
    CancellationToken,
}