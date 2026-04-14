namespace ACore.Abstractions;

/// <summary>
/// Common interface for all object that can be ran
/// </summary>
public interface IRunnable
{
    /// <summary>
    /// Run object
    /// </summary>
    Task Run(CancellationToken token = default);
}