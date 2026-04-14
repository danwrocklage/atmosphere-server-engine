namespace ACore.Abstractions;

/// <summary>
/// Common info about cell and its environment
/// </summary>
public interface ICellEnvironment
{
    /// <summary>
    /// High level role of cell
    /// </summary>
    string Role { get; }
    
    /// <summary>
    /// Cell environment configuration
    /// </summary>
    /// <remarks>
    /// Configurations are:
    /// - Development
    /// - Staging
    /// - Production
    /// </remarks>
    string Configuration { get; }
    
    /// <summary>
    /// Cell current build version
    /// </summary>
    string Build { get; }
    
    /// <summary>
    /// Cell endpoint
    /// </summary>
    string Endpoint { get; }
    
    /// <summary>
    /// True, if cell is running in docker container
    /// </summary>
    bool IsContainerBuild { get; }

    /// <summary>
    /// Get string with all cell information
    /// </summary>
    public string ToString(bool isFullFormat) => 
        isFullFormat ?
            $"Cell {(IsContainerBuild ? "(containerized)" : string.Empty)}: {Role}.{Configuration}.{Build}. Endpoint: {Endpoint}":
            $"{Role}.{Configuration}.{Build}";
}