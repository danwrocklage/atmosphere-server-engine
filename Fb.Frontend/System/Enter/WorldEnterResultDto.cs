using AUtils.Math;
using AUtils.Sil;

namespace Fb.Frontend;

[Sil(128)]
public class WorldEnterResultDto
{
    public Point3 Position { get; set; }
    
    public Point3 Direction { get; set; }
    
    public string[] MorphTargets { get; set; }
}