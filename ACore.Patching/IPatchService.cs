namespace ACore.Patching;

/// <summary>
/// Service for updating global application state (patches)
/// </summary>
public interface IPatchService
{
    /// <summary>
    /// Update application to newest state (patch) by <paramref name="category"/>
    /// </summary>
    Task Migrate(string category, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update application to specified <paramref name="destination"/> patch by <paramref name="category"/>
    /// </summary>
    Task Migrate(string category, string destination, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all available patches (stored in DB and found CLR types) by <paramref name="category"/>
    /// </summary>
    Task<PatchInfo[]> GetPatches(string category = null, CancellationToken cancellationToken = default);
}