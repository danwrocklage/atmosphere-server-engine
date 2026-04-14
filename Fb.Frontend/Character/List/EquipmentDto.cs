using AUtils.Sil;

namespace Fb.Frontend.Character;

[Sil(135)]
public class EquipmentDto
{
    public string Mesh { get; set; }
    
    public string Type { get; set; }
    
    public string[] Parameters { get; set; }
}