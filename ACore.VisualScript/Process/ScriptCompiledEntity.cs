using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace ACore.VisualScript;

[Table("visualscript.compiled")]
internal class ScriptCompiledEntity : IDbEntity
{
    /// <summary>
    /// Identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Serialized as JSON script expression
    /// </summary>
    public string? JsonCode { get; set; }
    
    /// <summary>
    /// True, if method is asynchronous
    /// </summary>
    public bool IsAsync { get; set; }
        
    /// <summary>
    /// When updated last time
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}