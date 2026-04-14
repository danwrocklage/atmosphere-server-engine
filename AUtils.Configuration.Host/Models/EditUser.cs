using AUtils.Configuration.Host.Database;

namespace AUtils.Configuration.Host.Models;

/// <summary>
/// Model for editing users
/// </summary>
public class EditUser
{
    /// <summary>
    /// New user name
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// New user type
    /// </summary>
    public UserType Type { get; set; }
}