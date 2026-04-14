using AUtils.Sil;

namespace Fb.Frontend.Character;

[Sil(137)]
public class CreateCharacterDto
{
    public string Name { get; set; }
    
    public Dictionary<string, float> MorphTargets { get; set; }
}