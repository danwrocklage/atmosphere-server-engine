using System.ComponentModel.DataAnnotations;

namespace AUtils.Configuration.Host.Database;

/// <summary>
/// User model in database
/// </summary>
public class User
{
    /// <summary>
    /// Identifier
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// User name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// User secret token
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// User type
    /// </summary>
    public UserType Type { get; set; }
    
    /// <summary>
    /// When user was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Last user update
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}