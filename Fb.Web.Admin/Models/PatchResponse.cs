namespace Fb.Web.Admin.Models;

public class PatchResponse
{
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
        
    /// <summary>
    /// Available CLR type of this patch
    /// </summary>
    public bool HasInCode { get; set; }
}