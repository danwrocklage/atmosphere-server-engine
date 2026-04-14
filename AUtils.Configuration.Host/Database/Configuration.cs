using System.ComponentModel.DataAnnotations;

namespace AUtils.Configuration.Host.Database;

/// <summary>
/// Json configuration for cells
/// </summary>
public class Configuration
{
    /// <summary>
    /// Type of cell
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Cell environment configuration
    /// </summary>
    [MaxLength(100)]
    public string? Environment { get; set; }
    
    /// <summary>
    /// Configuration itself
    /// </summary>
    [Required]
    public string Json { get; set; } = string.Empty;
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Last configuration update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}