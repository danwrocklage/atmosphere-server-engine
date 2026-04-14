namespace ACore.Patching;

public class PatchInfo
{
    internal PatchInfo() {}
    
    /// <summary>
    /// Id
    /// </summary>
    public Guid? Id { get; internal init; }

    /// <summary>
    /// Patch name from <see cref="System.ComponentModel.DescriptionAttribute"/>
    /// </summary>
    public string Name { get; internal init; }
        
    /// <summary>
    /// When it was be applied (null if not)
    /// </summary>
    public DateTime? AppliedAt { get; internal init; }

    /// <summary>
    /// CLR type
    /// </summary>
    public string ClrType { get; internal init; }
        
    /// <summary>
    /// Patch unique name with format yyyyMMdd_number
    /// </summary>
    public string Order { get; internal init; }

    /// <summary>
    /// Patch category
    /// </summary>
    public string Category { get; internal init; }
        
    /// <summary>
    /// Available CLR type of this patch
    /// </summary>
    public bool HasInCode { get; internal init; }
}