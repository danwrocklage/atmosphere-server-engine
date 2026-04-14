namespace ACore.Abstractions;

/// <summary>
/// Interface for all objects, which are needed for start preparation. Async version
/// </summary>
public interface IAsyncInitializable
{
    /// <summary>
    /// Preparation for start
    /// </summary>
    Task InitializeAsync();
}