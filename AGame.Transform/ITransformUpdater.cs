using AUtils.Math;

namespace AGame.Transform;

/// <summary>
/// Update actor position for global system
/// </summary>
public interface ITransformUpdater
{
    /// <summary>
    /// Update actor position
    /// </summary>
    void Update(in Guid actorId, in Point3 position);

    /// <summary>
    /// Remove actor position
    /// </summary>
    void Remove(in Guid actorId);
}