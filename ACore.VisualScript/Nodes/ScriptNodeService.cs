using System.Text.RegularExpressions;
using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using ACore.VisualScript.Models;

namespace ACore.VisualScript;

/// <inheritdoc />
[Log(Category = "vs.nodes")]
internal class ScriptNodeService : IScriptNodeService
{
    private static readonly Regex sColorRegex = new("^#[a-fA-F0-9]{6}$", RegexOptions.Compiled);
    private static readonly Regex sTypeRegex = new("^[-.a-fA-F0-9]+$", RegexOptions.Compiled);
        
    private readonly IDatabase mDatabase;
    private readonly ILogger<ScriptNodeService> mLogger;

    public ScriptNodeService(IDatabase database, ILogger<ScriptNodeService> logger)
    {
        mDatabase = database;
        mLogger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ScriptNodeView>> Get(ScriptNodeFilter filter)
    {
        if (filter == null) 
            throw new ArgumentNullException(nameof(filter));
            
        if(string.IsNullOrEmpty(filter.Context))
            throw new ArgumentNullException(nameof(filter.Context));

        var query = mDatabase.Select<ScriptNodeEntity>()
            .Where(x => x.Contexts.Contains(filter.Context));
            
        if (!string.IsNullOrEmpty(filter.Type))
        {
            query = filter.IsForward ? 
                query.Where(x => x.Input.Any(i => i.Type == filter.Type)) :
                query.Where(x => x.Output.Any(i => i.Type == filter.Type));
        }

        if (!string.IsNullOrEmpty(filter.Search))
            query = query.Where(x => x.Name.Contains(filter.Search) || x.Description.Contains(filter.Search));

        if (filter.HasFlowIn.HasValue)
            query = query.Where(x => x.Input.Any(a => a.IsFlow));
            
        if (filter.HasFlowOut.HasValue)
            query = query.Where(x => x.Output.Any(a => a.IsFlow));
            
        return await query
            .Select(x => new ScriptNodeView
            {
                Name = x.Name,
                Color = x.Color,
                Description = x.Description,
                Group = x.Group,
                Input = x.Input,
                Output = x.Output,
                Type = x.Type
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> Create(ScriptNodeEdit model)
    {
        if(!await Validate(model))
        {
            mLogger.Warn("Failed to create new node description");
            return false;
        }

        mLogger.Info($"Create new node description '{model.Name}' TYPE:{model.Type}");
        await mDatabase.Repository<ScriptNodeEntity>()
            .Insert(new ScriptNodeEntity
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                Color = model.Color,
                Contexts = model.Contexts,
                Description = model.Description,
                Group = model.Group,
                Input = model.Input,
                Output = model.Output,
                Tags = model.Tags,
                Type = model.Type
            });

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> Update(Guid id, ScriptNodeEdit model)
    {
        if(!await Validate(model))
        {
            mLogger.Warn("Failed to update node description");
            return false;
        }

        mLogger.Info($"Update node description '{model.Name}' TYPE:{model.Type} ID:{id.ToString()}");
        await mDatabase.Repository<ScriptNodeEntity>()
            .Update(new ScriptNodeEntity
            {
                Id = id,
                Name = model.Name,
                Color = model.Color,
                Contexts = model.Contexts,
                Description = model.Description,
                Group = model.Group,
                Input = model.Input,
                Output = model.Output,
                Tags = model.Tags,
                Type = model.Type
            });

        return true;
    }

    /// <inheritdoc />
    public async Task Delete(Guid id)
    {
        mLogger.Info($"Try to delete node description with ID:{id.ToString()}");
        await mDatabase.Repository<ScriptNodeEntity>()
            .Delete(x => x.Id == id);
    }

    private async Task<bool> Validate(ScriptNodeEdit model)
    {
        if (string.IsNullOrEmpty(model.Type) || !sTypeRegex.IsMatch(model.Type) || 
            !await mDatabase.Select<ScriptNodeEntity>().AnyAsync(x => x.Type == model.Type))
            return false;

        if (string.IsNullOrEmpty(model.Name)) return false;
        if (string.IsNullOrEmpty(model.Description)) return false;
        if (string.IsNullOrEmpty(model.Group)) return false;
        if (string.IsNullOrEmpty(model.Color)) return false;
        if (!sColorRegex.IsMatch(model.Color)) return false;
            
        if (model.Contexts.Length < 1 || 
            model.Contexts.Any(x => string.IsNullOrEmpty(x))) return false;

        if (model.Input.Length == 0 && model.Output.Length == 0)
            return false;

        if (model.Input.Length > 0)
        {
            foreach (var endpoint in model.Input)
            {
                if (!await Validate(endpoint))
                    return false;
            }
        }
            
        if (model.Output.Length > 0)
        {
            foreach (var endpoint in model.Output)
            {
                if (!await Validate(endpoint))
                    return false;
            }
        }
            
        return true;
    }

    private async Task<bool> Validate(NodeEndpoint model)
    {
        if (string.IsNullOrEmpty(model.Type) || 
            !await mDatabase.Select<ScriptNodeEntity>().AnyAsync(x => x.Type == model.Type))
            return false;

        if (string.IsNullOrEmpty(model.Name)) return false;
        if (string.IsNullOrEmpty(model.Description)) return false;
        if (string.IsNullOrEmpty(model.Color)) return false;
        if (!sColorRegex.IsMatch(model.Color)) return false;

        return true;
    }
}