using ACore.VisualScript.Models;

namespace ACore.VisualScript;

/// <summary>
/// Service for managing visual script nodes in admin UI
/// </summary>
public interface IScriptNodeService
{
    /// <summary>
    /// Get all available nodes by specified filter
    /// </summary>
    Task<List<ScriptNodeView>> Get(ScriptNodeFilter filter);
        
    /// <summary>
    /// Create new node
    /// </summary>
    Task<bool> Create(ScriptNodeEdit model);

    /// <summary>
    /// Edit node
    /// </summary>
    Task<bool> Update(Guid id, ScriptNodeEdit model);

    /// <summary>
    /// Remove node
    /// </summary>
    Task Delete(Guid id);
}