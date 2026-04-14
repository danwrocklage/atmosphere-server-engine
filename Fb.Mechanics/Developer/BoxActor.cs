using AGame.Actors;
using AGame.Transform;

namespace Fb.Mechanics.Developer;

public class BoxActor : Actor
{
    public BoxActor()
    {
        TickingMode = TickingMode.NoTicking;
    }

    protected override Task OnCreate()
    {
        Add<TransformComponent>();
        Add<VisualStateComponent>();
        return Task.CompletedTask;
    }
}