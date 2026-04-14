using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using ACore.VisualScript.Models;

namespace ACore.VisualScript;

internal class ScriptService : IScriptService
{
    private readonly IDatabase mDatabase;
    private readonly ILogger<ScriptService> mLogger;

    public ScriptService(IDatabase database, ILogger<ScriptService> logger)
    {
        mDatabase = database;
        mLogger = logger;
    }

    public async Task<Guid> Save(Script schema, Guid? schemaId, Guid authorId)
    {
        if (!schemaId.HasValue)
        {
            var newId = Guid.NewGuid();
            await mDatabase.Repository<ScriptEntity>()
                .Insert(new ScriptEntity
                {
                    Id = newId,
                    Group = schema.Group,
                    Name = schema.Name,
                    AuthorId = authorId,
                    CreatedAt = DateTime.UtcNow,
                    IsCompiled = false,
                    UpdatedAt = DateTime.UtcNow,
                    Items = schema.Items
                });
                
            mLogger.Info($"New schema {schema.Group}.{schema.Name} was successfully created");
            return newId;
        }

        await mDatabase.Repository<ScriptEntity>().Update(schemaId.Value)
            .Set(x => x.Items, schema.Items)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.Name, schema.Name)
            .Set(x => x.Group, schema.Group)
            .Apply();
            
        mLogger.Info($"Schema {schema.Group}.{schema.Name} was successfully updated");
            
        return schemaId.Value;
    }

    public async Task<Script?> Get(Guid id)
    {
        return await mDatabase.Select<ScriptEntity>()
            .Where(x => x.Id == id)
            .Select(x => new Script
            {
                Group = x.Group,
                Name = x.Name,
                Items = x.Items
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<ScriptShort>> Get(ScriptFilter filter)
    {
        var schemasQuery = mDatabase.Select<ScriptEntity>();

        if (!string.IsNullOrEmpty(filter.Group))
            schemasQuery = schemasQuery.Where(x => x.Group == filter.Group);

        if (!string.IsNullOrEmpty(filter.Search))
            schemasQuery =
                schemasQuery.Where(x => x.Name.Contains(filter.Search) || x.Group != null && x.Group.Contains(filter.Search));

        return await schemasQuery.Select(x => new ScriptShort
        {
            Id = x.Id,
            Group = x.Group,
            Name = x.Name,
            IsCompiled = x.IsCompiled,
            ItemsCount = x.Items.Count() // Don't touch
        }).ToListAsync();
    }

    public async Task Delete(Guid schemaId, Guid authorId)
    {
        await mDatabase.Repository<ScriptEntity>()
            .Delete(x => x.Id == schemaId);
            
        mLogger.Info($"Schema {schemaId.ToString()} was successfully deleted");
    }

    public async Task<bool> Validate(Script schema)
    {
        if (schema == null || string.IsNullOrEmpty(schema.Name) || 
            schema.Items == null || schema.Items.Count == 0)
            return false;

        var ids = schema.Items.Select(x => x.Id).Distinct().Where(x => !string.IsNullOrEmpty(x)).ToHashSet();
        if (ids.Count != schema.Items.Count)
            return false;

        if (schema.Items.Any(x => x.Connections == null || x.Connections.Length == 0))
            return false;

        if (schema.Items.Any(x => string.IsNullOrEmpty(x.Type)))
            return false;

        if (schema.Items.Any(x => x.Values.IntersectBy(x.Connections.Select(c => c.Name), k => k.Key).Any()))
            return false;

        if (schema.Items.Any(x => x.Connections.Any(c => 
                !ids.Contains(c.NodeId) || 
                string.IsNullOrEmpty(c.Name) || 
                !c.IsOutput && string.IsNullOrEmpty(c.EndpointName))))
            return false;

        var nodeTypes = schema.Items.Select(x => x.Type).Distinct().ToArray();
        if (await mDatabase.Select<ScriptNodeEntity>().Where(x => nodeTypes.Contains(x.Type)).CountAsync() !=
            nodeTypes.Length)
            return false;

        return true;
    }
}