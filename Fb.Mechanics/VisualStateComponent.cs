using AGame.Actors;
using AGame.Actors.Persistence;

namespace Fb.Mechanics;

/// <summary>
/// Component for store client render data
/// </summary>
public class VisualStateComponent : ActorComponent
{
    private string mMesh;
    private string mState;

    [Persistence]
    public string Mesh
    {
        get => mMesh;
        set
        {
            Owner.Replicate("visualstate.mesh", value);
            mMesh = value;
        }
    }
    
    [Persistence]
    public string State
    {
        get => mState;
        set
        {
            Owner.Replicate("visualstate.state", value);
            mState = value;
        }
    }
}