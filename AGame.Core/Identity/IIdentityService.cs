using ACore.Abstractions.Database;

namespace AGame.Core.Identity;

/// <summary>
/// Service to work with basic authentication entities
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Create new identity (low level authentication entity)
    /// </summary>
    /// <returns>Created identity id</returns>
    Task<Guid> Create(string key, string secret, IdentityType type, string[] grandTypes);

    /// <summary>
    /// Return true, if an identity of specified type and key exists 
    /// </summary>
    Task<bool> Exists(string key, IdentityType type);

    /// <summary>
    /// Get identity by specified id
    /// </summary>
    Task<Identity> Get(Guid id);

    /// <summary>
    /// Connect identity with user entity (staff, player, etc.)
    /// </summary>
    /// <param name="identityId">Identity id</param>
    /// <param name="linkedEntityId">Id of entity</param>
    /// <param name="linkType">CLR type of linked entity</param>
    Task Link(Guid identityId, Guid linkedEntityId, string linkType);

    /// <summary>
    /// Connect identity with user entity (staff, player, etc.)
    /// </summary>
    /// <param name="identityId">Identity id</param>
    /// <param name="entity">Linked entity</param>
    public Task Link<T>(Guid identityId, T entity) where T : IDbEntity =>
        Link(identityId, entity.Id, typeof(T).FullName);

    /// <summary>
    /// Remove connection with user entity
    /// </summary>
    /// <param name="identityId">Identity id</param>
    /// <param name="linkedEntityId">Id of entity</param>
    Task RemoveLink(Guid identityId, Guid linkedEntityId);
    
    /// <summary>
    /// Get existed identity of specified public and secret parts
    /// </summary>
    Task<(Identity Identity, bool ShouldBeBlocked)> Authorize(string @public, string @private, bool countFails);

    /// <summary>
    /// Set fails to 0 for identity
    /// </summary>
    Task ResetFailsCounter(Guid identityId);
}