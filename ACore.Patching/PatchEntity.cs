using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace ACore.Patching;

/// <summary>
/// Model for store patch info in database
/// </summary>
[Table("patches")]
internal class PatchEntity : IDbEntity
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Patch name from <see cref="System.ComponentModel.DescriptionAttribute"/>
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// When it was be applied (null if not)
    /// </summary>
    public DateTime? AppliedAt { get; set; }

    /// <summary>
    /// CLR type
    /// </summary>
    public string ClrType { get; set; }

    /// <summary>
    /// Patch unique name with format yyyyMMdd_number
    /// </summary>
    public string Order { get; set; }

    /// <summary>
    /// Patch category
    /// </summary>
    public string Category { get; set; }
}