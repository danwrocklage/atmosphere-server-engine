namespace ACore.Abstractions;

/// <summary>
/// Interface for all objects, which are needed for start preparation
/// </summary>
public interface IInitializable
{
    /// <summary>
    /// Preparation for start
    /// </summary>
    void Initialize();
}