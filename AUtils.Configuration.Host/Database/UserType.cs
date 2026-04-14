namespace AUtils.Configuration.Host.Database;

/// <summary>
/// Type of users
/// </summary>
public enum UserType : byte
{
    /// <summary>
    /// User which can do all
    /// </summary>
    Administrator,
    
    /// <summary>
    /// User for only reading configurations
    /// </summary>
    Cell
}