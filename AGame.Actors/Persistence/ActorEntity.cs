using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;
using MongoDB.Bson;

namespace AGame.Actors.Persistence;

/// <summary>
/// Model for storage actor in database
/// </summary>
[Table("actors")]
internal class ActorEntity : IDbEntity
{
    /// <summary>
    /// Entity id
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Actor typeof().FullName
    /// </summary>
    public string Type { get; set; }
    
    /// <summary>
    /// Cell Id where actor is living (null if not loaded)
    /// </summary>
    public Guid? AppId { get; set; }
    
    /// <summary>
    /// Actor group id
    /// </summary>
    public string MechanicsId { get; set; }
    
    /// <summary>
    /// Where actor lives
    /// </summary>
    public Guid WorldId { get; set; }
    
    /// <summary>
    /// Actor name
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Parent actor id
    /// </summary>
    public Guid? ParentId { get; set; }
    
    /// <summary>
    /// Children actors ids
    /// </summary>
    public Guid[] ChildrenIds { get; set; }
    
    /// <summary>
    /// Type of actor ticking
    /// </summary>
    public TickingMode TickingMode { get; set; }
    
    /// <summary>
    /// Is actor receiving events?
    /// </summary>
    public bool IsEventReceiver { get; set; }
    
    /// <summary>
    /// Other actor properties, which required for store
    /// </summary>
    public BsonDocument Properties { get; set; }
    
    /// <summary>
    /// Actor's components, prepared for store
    /// </summary>
    public ActorEntityComponent[] Components { get; set; }
    
    /// <summary>
    /// Store timestamp
    /// </summary>
    public DateTime StoredAt { get; set; }
}

/// <summary>
/// Model for store component in database
/// </summary>
internal class ActorEntityComponent
{
    /// <summary>
    /// Component typeof().FullName
    /// </summary>
    public string Type { get; set; }
    
    /// <summary>
    /// Component name
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Should component run update cycle
    /// </summary>
    public bool IsTicking { get; set; }
    
    /// <summary>
    /// Other component properties, which required for store
    /// </summary>
    public BsonDocument Properties { get; set; }
}