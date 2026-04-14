namespace Fb.Web.Portal.Models;

public class CharacterCreateRequest
{
    public string Name { get; set; }
    
    public Dictionary<string, float> MorphTargets { get; set; }
}