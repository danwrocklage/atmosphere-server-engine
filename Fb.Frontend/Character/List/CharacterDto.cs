using AUtils.Sil;

namespace Fb.Frontend.Character;

[Sil(134)]
public class CharacterDto
{
    public Guid Id { get; set; }
    
    public string Name { get; set; }
    
    public string[] MorphTargets { get; set; }
    
    public string Mesh { get; set; }
    
    public DateTime? LastSeenOnline { get; set; }
    
    public EquipmentDto[] Items { get; set; }
}