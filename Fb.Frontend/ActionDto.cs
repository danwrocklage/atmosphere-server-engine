using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using AUtils.Math;
using AUtils.Sil;

namespace Fb.Frontend;

[Sil(152)]
public struct ParametersDto
{
    public bool? ForceClientCache { get; set; }
    
    public bool? EnableChatNotifications { get; set; }
    
    public bool? ForceEnvironment { get; set; }
}

[Sil(147)]
public struct ActionDto
{
    public static readonly object Idle = new ActionDto {Type = ActionType.Idle, Direction = null};
    
    public Point3? Direction { get; set; }
    
    public ActionType? Type { get; set; }
    
    public ParametersDto? Parameters { get; set; }
}

[Sil(149)]
public struct InteractionDto
{
    public Point3? Direction { get; set; }
    
    public Guid ActorId { get; set; }
}

[Description("Type of player action")]
public enum ActionType : byte
{
    [Display(Name = "Do nothing")]
    Idle,
    [Display(Name = "Walking")]
    Walk,
    [Display(Name = "Small running")]
    Run,
    [Display(Name = "Fast running")]
    Sprint,
    [Display(Name = "Jump")]
    Jump,
    [Display(Name = "Crouch")]
    Crouch
}

[Sil(131)]
public class FireActionDto
{
    public Point3 Direction { get; set; }

    public FireType Type { get; set; }
}

[Description("Type of player fire")]
public enum FireType : byte
{
    [Display(Name = "Primary weapon attack")]
    Primary,
    [Display(Name = "Alternative weapon attack (if exists)")]
    Additional
}

public class SkillActionDto
{
    public Point3 Direction { get; set; }
    
    public int SkillId { get; set; }
}