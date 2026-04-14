using ACore.VisualScript.Models;

namespace ACore.VisualScript;

/// <summary>
/// Service for managing visual script schemas
/// </summary>
public interface IScriptService
{
    /// <summary>
    /// Validate schema
    /// </summary>
    Task<bool> Validate(Script schema);
        
    /// <summary>
    /// Store visual script
    /// </summary>
    /// <param name="schema">Script</param>
    /// <param name="scriptId">Script id. Null for create new</param>
    /// <param name="authorId">User staff id</param>
    /// <returns></returns>
    Task<Guid> Save(Script schema, Guid? scriptId, Guid authorId);

    /// <summary>
    /// Get stored script by id
    /// </summary>
    Task<Script?> Get(Guid id);

    /// <summary>
    /// Get scripts list by specified filter
    /// </summary>
    Task<List<ScriptShort>> Get(ScriptFilter filter);

    /// <summary>
    /// Delete script
    /// </summary>
    Task Delete(Guid schemaId, Guid authorId);
}