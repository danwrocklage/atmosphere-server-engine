namespace AGame.Core.Identity;

/// <summary>
/// Custom claim types for engine
/// </summary>
public static class ClaimTypes
{
    /// <summary>
    /// Store base entity identifier
    /// </summary>
    public static string EntityId => "Entity";
    
    /// <summary>
    /// Store access scopes (for staff)
    /// </summary>
    public static string Scopes => "Scopes";
    
    /// <summary>
    /// Store type of base entity (Type.FullName)
    /// </summary>
    public static string EntityType => "EntityType";
    
    /// <summary>
    /// Store type of access for entity
    /// </summary>
    public static string GrandType => "GrandType";

    /// <summary>
    /// Entry queue position
    /// </summary>
    public static string Queue => "Queue";
    
    /// <summary>
    /// Store type of client application
    /// </summary>
    public static string ClientType => "ClientType";
}