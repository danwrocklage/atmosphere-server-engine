using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;
using Fb.Mechanics.Stats;

namespace Fb.Mechanics.PlayerCharacter;

[Table("characters")]
public class CharacterEntity : IDbEntity
{
    public Guid Id { get; set; }
    
    public Guid AccountId { get; set; }
    
    public string Name { get; set; }
    
    public string[] MorphTargets { get; set; }
    
    public string Mesh { get; set; }
    
    public string State { get; set; }
    
    public float[] Position { get; set; }
    
    public float[] Direction { get; set; }
    
    public Dictionary<StatType, int> Stats { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? LastSeenOnline { get; set; }
    
    public bool IsOnline { get; set; }
}