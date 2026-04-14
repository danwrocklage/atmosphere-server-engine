namespace AGame.Core.ClientApp;

/// <summary>
///     Type for all
/// </summary>
public enum ClientBuildType : byte
{
    /// <summary>
    ///     Early stage build for QA
    /// </summary>
    Develop,
    
    /// <summary>
    ///     Build for public test
    /// </summary>
    Beta,
    
    /// <summary>
    ///     Release build for all
    /// </summary>
    Public
}