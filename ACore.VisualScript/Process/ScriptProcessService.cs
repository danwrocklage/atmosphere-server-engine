using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using ACore.Abstractions.Database;
using ACore.Abstractions.Rpc;
using ACore.VisualScript.Models;
using AUtils.Expressions.Json;
using AUtils.IoC;

namespace ACore.VisualScript;

internal class ScriptProcessService : IScriptProcessService
{
    internal static readonly ConcurrentDictionary<Guid, (DateTime, Delegate)> CompiledScripts = new();

    private readonly JsonSerializerOptions? mJsonOptions;
    private readonly IDatabase mDatabase;
    private readonly IContainer mContainer;
    private readonly IRpc mRpc;

    public ScriptProcessService(IDatabase database, IContainer container, IRpc rpc)
    {
        mDatabase = database;
        mContainer = container;
        mRpc = rpc;
        mJsonOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
            Converters =
            {
                new LambdaExpressionConverter(),
                new ExpressionConverter(container),
                new MethodInfoConverter(),
                new PropertyInfoConverter(),
                new ConstructorInfoConverter(),
                new LabelTargetConverter(),
                new TypeConverter()
            }
        };
    }
        
    public async Task Compile(Guid scriptId, CancellationToken token = default)
    {
        var units = await GetUnitsFromSchema(scriptId, token);
        if(units.Count == 0)
            return;

        var resolver = new NodeResolver(mContainer, units);
        var (method, isAsync) = await resolver.Resolve();
        var json = JsonSerializer.Serialize(method, mJsonOptions);

        await mDatabase.Repository<ScriptEntity>()
            .Update(scriptId) 
            .Set(x => x.IsCompiled, true)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Apply(token: token);

        var codeEntity = await mDatabase.Select<ScriptCompiledEntity>()
            .FirstOrDefaultAsync(x => x.Id == scriptId, token);

        if (codeEntity == null)
            await mDatabase.Repository<ScriptCompiledEntity>().Insert(new ScriptCompiledEntity
            {
                JsonCode = json,
                Id = scriptId,
                UpdatedAt = DateTime.UtcNow
            }, token);
        else
            await mDatabase.Repository<ScriptCompiledEntity>()
                .Update(scriptId)
                .Set(x => x.JsonCode, json)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Apply(token: token);

        await mRpc.Call(new ScriptChangedEvent {ScriptId = scriptId}, token);
    }

    public async Task Execute(Guid scriptId, object?[]? args = null, CancellationToken token = default)
    {
        var codeEntityUpdated = await mDatabase.Select<ScriptCompiledEntity>()
            .Where(x => x.Id == scriptId)
            .Select(x => x.UpdatedAt)
            .SingleOrDefaultAsync(token);

        if (codeEntityUpdated == default)
            throw new InvalidOperationException();

        if (CompiledScripts.TryGetValue(scriptId, out var script) && script.Item1 >= codeEntityUpdated)
        {
            script.Item2.DynamicInvoke(args);
            return;
        }
            
        var entityJsonCode = await mDatabase.Select<ScriptCompiledEntity>()
            .Where(x => x.Id == scriptId)
            .Select(x => x.JsonCode)
            .SingleOrDefaultAsync(token);
            
        if (entityJsonCode == null)
            throw new InvalidOperationException();

        var action = JsonSerializer.Deserialize<LambdaExpression>(entityJsonCode, mJsonOptions);
        var @delegate = action?.Compile() ?? throw new InvalidOperationException();

        CompiledScripts.AddOrUpdate(scriptId, (codeEntityUpdated, @delegate), (_, _) => (codeEntityUpdated, @delegate));

        CompiledScripts[scriptId].Item2.DynamicInvoke(args);
    }

    private async Task<IReadOnlyCollection<NodeUnit>> GetUnitsFromSchema(Guid schemaId, CancellationToken token = default)
    {
        var schema = await mDatabase.Select<ScriptEntity>()
            .FirstOrDefaultAsync(x => x.Id == schemaId, token);

        if (schema == null)
            return Array.Empty<NodeUnit>();

        var schemaDescriptionsIds = schema.Items.Select(x => x.Type).Distinct().ToHashSet();
        var descriptions = (await mDatabase.Select<ScriptNodeEntity>()
                .Where(x => schemaDescriptionsIds.Contains(x.Type))
                .Select(x => new NodeUnitDescription
                {
                    Type = x.Type,
                    Input = x.Input,
                    Output = x.Output
                })
                .ToListAsync(token))
            .ToDictionary(x => x.Type, x => x);

        var notFoundTypes = descriptions.Keys.Where(x => !schemaDescriptionsIds.Contains(x)).ToArray();
        if (notFoundTypes.Length != 0)
            throw new InvalidOperationException($"Node types ({string.Join(",", notFoundTypes)}) were not found");

        var converter = new NodeConverter(schema.Items, descriptions);
        converter.Convert();

        return converter.Result.Values.ToArray();
    }
}