using System.Diagnostics;
using System.Text.Json.Serialization;

namespace AGame.Actors;

/// <summary>
/// Actor component
/// </summary>
[DebuggerDisplay("{Name}")]
public abstract class ActorComponent
{
    /// <summary>
    /// Actor which contains this component
    /// </summary>
    [JsonIgnore]
    public Actor Owner { get; internal set; }
    
    /// <summary>
    /// Component name (if has)
    /// </summary>
    public string Name { get; internal set; }
    
    /// <summary>
    /// Is component updating
    /// </summary>
    protected internal bool IsTicking { get; set; }
    
    /// <summary>
    /// Event when component was added to an actor
    /// </summary>
    protected internal virtual Task Attach() => Task.CompletedTask;
    
    /// <summary>
    /// Event when component was removed from actor
    /// </summary>
    /// <param name="isDestroying">true, if component is detaching because actor is destroying</param>
    protected internal virtual Task Detach(bool isDestroying) => Task.CompletedTask;
    
    /// <summary>
    /// Component update cycle
    /// </summary>
    protected internal virtual void Tick(TimeSpan delta) { }
}