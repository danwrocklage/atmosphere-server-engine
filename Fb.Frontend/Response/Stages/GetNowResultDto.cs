using AGame.Time;
using AUtils.Sil;

namespace Fb.Frontend.System.Time;

[Sil(144)]
public struct GetNowResultDto
{
    public DateTime UtcNow { get; set; }
    
    public GameTime GameNow { get; set; }
}