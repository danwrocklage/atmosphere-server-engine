using ACore.Abstractions;
using ACore.Abstractions.Rpc;
using AUtils.Math;
using AUtils.SpacialDatabase;

namespace AGame.Transform;

/// <summary>
/// Service for manage actors positions
/// </summary>
internal class TransformService : ITransformUpdater, ITransformService
{
    private readonly SpacialDatabase<Guid> mActorsSpacialDb;
    private readonly IRpc mRpc;

    public TransformService(IConfiguration configuration, IRpc rpc)
    {
        var config = configuration.Get(() => SpacialDatabaseConfiguration.Default);
        mActorsSpacialDb = new SpacialDatabase<Guid>(config.CellSize);
        mRpc = rpc;
    }

    internal void InternalUpdate(in Guid actorId, in Point3 position) => 
        mActorsSpacialDb.AddOrUpdate(actorId, position);

    /// <inheritdoc />
    public void Update(in Guid actorId, in Point3 position)
    {
        InternalUpdate(actorId, position);
        mRpc.Call(new ActorTransform {Position = position, ActorId = actorId});
    }
    
    internal void InternalRemove(in Guid actorId) => 
        mActorsSpacialDb.Remove(actorId);

    /// <inheritdoc />
    public void Remove(in Guid actorId)
    {
        InternalRemove(actorId);
        mRpc.Call(new ActorTransformRemove {ActorId = actorId});
    }

    /// <inheritdoc />
    public Guid[] GetByRadius(in Point3 center, in double radius)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, Point3> GetByRect(in Point3 center, in float centerToTop, in float centerToLeft)
    {
        return mActorsSpacialDb.GetByRect(center, centerToTop, centerToLeft);
    }
    
    #region Utils

    /// <summary>
    /// Service configuration model
    /// </summary>
    [Configuration("spacial.db")]
    private class SpacialDatabaseConfiguration
    {
        /// <summary>
        /// Size of cell size in <see cref="SpacialDatabase{T}"/>
        /// </summary>
        public float CellSize { get; set; }

        public static SpacialDatabaseConfiguration Default => new()
        {
            CellSize = 5000
        };
    }
    
    #endregion
}