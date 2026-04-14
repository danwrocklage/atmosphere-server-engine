using AUtils.Math;

namespace AGame.Transform;

/// <summary>
/// Service for query actors positions
/// </summary>
public interface ITransformService
{
    /// <summary>
    /// Get actors positions by circle
    /// </summary>
    Guid[] GetByRadius(in Point3 center, in double radius);
    
    /// <summary>
    /// Get actors positions by rect
    /// </summary>
    IReadOnlyDictionary<Guid, Point3> GetByRect(in Point3 center, in float centerToTop, in float centerToLeft);
}