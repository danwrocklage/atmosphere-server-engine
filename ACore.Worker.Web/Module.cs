namespace ACore.Worker.Web;

/// <summary>
/// Requests processing module
/// </summary>
public abstract class Module
{
    /// <summary>
    /// Configure http request processing pipeline
    /// </summary>
    public abstract void Configure(PipelineBuilder builder);
}