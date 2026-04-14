using AGame.Actors.Persistence;
using AGame.Transform;
using AUtils.Math;
using Fb.Mechanics.Stats;

namespace Fb.Mechanics;

public enum CharacterActionType : byte
{
    Idle,
    Walk,
    Run,
    Sprint,
    Crouch
}

public class CharacterTransformComponent : TransformComponent
{
    private readonly ITransformService mTransformService;
    
    public CharacterTransformComponent(ITransformUpdater transformService, ITransformService transformService1) : 
        base(transformService)
    {
        mTransformService = transformService1;
        ActionType = CharacterActionType.Idle;
    }

    [Persistence] 
    public CharacterActionType ActionType { get; set; }

    public void Look(Point3 newDirection)
    {
        var vecDirection = (Vector3) newDirection;
        vecDirection.Normalize();
        Direction = vecDirection;
    }

    public void Move(bool walk, bool sprint)
    {
        if (walk && sprint)
            throw new ArgumentException();
        
        var speed = Owner.Get<StatComponent>().Get(StatType.MovementSpeed);
        var currentSpeed = speed.Value * (walk ? 0.5f : sprint ? 2 : 1);
        var speedVec = (Vector3) Direction * currentSpeed;
        var newPosition = Position + speedVec;
        //var colliders = mTransformService.GetByRect()
    }

    public void Jump()
    {
        
    }

    public void TeleportTo(Point3 point)
    {
        Position = point;
        ActionType = CharacterActionType.Idle;
    }

    protected override void Tick(TimeSpan delta)
    {
        if(ActionType == CharacterActionType.Idle)
            return;
        
        
    }
}