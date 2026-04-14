using AGame.Actors;
using AGame.Actors.Persistence;
using AUtils.Math;

namespace AGame.Transform;

/// <summary>
/// Actor component for store and replicate actor position
/// </summary>
public class TransformComponent : ActorComponent
{
    private const float TOLERANCE = 0.0001f;

    private readonly ITransformUpdater mTransformService;
    private Point3 mPosition;
    private Point3 mDirection;

    public TransformComponent(ITransformUpdater transformService)
    {
        mTransformService = transformService;
    }

    protected override Task Attach()
    {
        mTransformService.Update(Owner.Id, mPosition);
        Direction = Point3.Empty;
        return Task.CompletedTask;
    }

    protected override Task Detach(bool isDestroying)
    {
        mTransformService.Remove(Owner.Id);
        return Task.CompletedTask;
    }

    [Persistence] 
    public Point3 Position
    {
        get => mPosition;
        set
        {
            if(Math.Abs(mPosition.X - value.X) > TOLERANCE || Math.Abs(mPosition.Y - value.Y) > TOLERANCE)
                mTransformService.Update(Owner.Id, value);
            mPosition = value;
        }
    }

    [Persistence]
    public Point3 Direction 
    { 
        get => mDirection;
        set
        {
            Owner.Replicate("transform.direction", value);
            mDirection = value;
        } 
    }
    
    [Persistence] 
    protected Point[] CollisionBox { get; set; }
}