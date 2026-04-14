namespace ACore.Abstractions;

public static class Cell
{
    public static Guid AppId { get; } = Guid.NewGuid();

    #region Configurations

    /// <summary>
    /// Cell application configuration for local running
    /// </summary>
    public const string CONFIGURATION_DEVELOPMENT = "Development";

    /// <summary>
    /// Cell application configuration for testing environment
    /// </summary>
    public const string CONFIGURATION_STAGING = "Staging";
    
    /// <summary>
    /// Cell application configuration for production
    /// </summary>
    public const string CONFIGURATION_PRODUCTION = "Production";

    #endregion

    #region Names

    /// <summary>
    /// Role for running all roles in one cell
    /// </summary>
    /// <remarks>ONLY FOR DEVELOPMENT</remarks>
    public const string DEV = "dev";
    
    /// <summary>
    /// Role for accepting players connections
    /// </summary>
    public const string FRONTEND = "frontend";
    
    /// <summary>
    /// Role for game rules
    /// </summary>
    public const string MECHANICS = "mechanics";
    
    /// <summary>
    /// Public API
    /// </summary>
    public const string PORTAL_API = "portal";
    
    /// <summary>
    /// Admin API
    /// </summary>
    public const string ADMIN_API = "admin";
    
    /// <summary>
    /// Role for data migration and patching
    /// </summary>
    public const string SEED = "seed";

    #endregion
}