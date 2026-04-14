using AUtils.Math;
using AUtils.Sil;

namespace Fb.Frontend;

[Sil(129)]
public class WorldViewDto
{
    public WorldViewItem[] Items { get; set; }
}

[Sil(130)]
public class WorldViewItem
{
    public Guid Id { get; set; }
    
    public string Mesh { get; set; }
    
    public string[] MorphTargets { get; set; }
    
    public string State { get; set; }
    
    public Point3 Position { get; set; }
    
    public Point3? Direction { get; set; }
    
    public bool Cached { get; set; }
}