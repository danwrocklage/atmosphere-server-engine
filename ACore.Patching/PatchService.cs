using System.ComponentModel;
using System.Reflection;
using ACore.Abstractions;
using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using IContainer = AUtils.IoC.IContainer;

namespace ACore.Patching;

/// <inheritdoc />
[Log(Category = "patch")]
internal class PatchService : IPatchService
{
    private readonly IDatabase mDatabase;
    private readonly ILogger<PatchService> mLogger;
    private readonly IContainer mContainer;

    public PatchService(IDatabase database, ILogger<PatchService> logger, IContainer container)
    {
        mDatabase = database;
        mLogger = logger;
        mContainer = container;
    }

    /// <inheritdoc />
    public async Task Migrate(string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(category)) 
            throw new ArgumentNullException(nameof(category));
            
        mLogger.Debug($"[{category}] Applying patches...");

        var patchTypes = Types.All
            .Where(x => x.BaseType == typeof(Patch) && !x.IsAbstract)
            .ToDictionary(x => x.FullName ?? string.Empty, x => x);

        var patchTypeNames = patchTypes.Keys.ToHashSet();
        var patches = (await mDatabase.Repository<PatchEntity>().Select()
                .Where(x => x.Category == category && patchTypeNames.Contains(x.ClrType))
                .OrderBy(x => x.Order)
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ClrType, x => x);

        var unAppliedPatches = patchTypes.Values
            .Where(x =>
                !patches.ContainsKey(x.FullName ?? string.Empty) ||
                patches[x.FullName ?? string.Empty].AppliedAt == null)
            .Select(x => new
            {
                Entity = patches.TryGetValue(x.FullName ?? string.Empty, out var entity) ? entity : null,
                Type = x,
                Object = (Patch) mContainer.Resolve(x)
            })
            .Where(x => x.Object.Category == category)
            .OrderBy(x => x.Object.Order)
            .ToArray();

        mLogger.Debug($"Found {unAppliedPatches.Length} patches to up");

        var hasError = false;
        foreach (var patch in unAppliedPatches)
        {
            var name = patch.Entity?.Name ?? 
                       patch.Type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? 
                       patch.Type.Name;
                
            mLogger.Info($"Applying patch '{name}' ({patch.Type.FullName}) ...");

            if (!patch.Object.TryParseOrder(out _))
                throw new InvalidDataException($"Invalid patch order {patch.Object.Order}");

            try
            {
                await patch.Object.Up();
                if (patch.Entity == null)
                    await mDatabase.Repository<PatchEntity>()
                        .Insert(new PatchEntity
                        {
                            Id = Guid.NewGuid(),
                            Name = name,
                            Order = patch.Object.Order,
                            AppliedAt = DateTime.UtcNow,
                            ClrType = patch.Type.FullName ?? string.Empty,
                            Category = patch.Object.Category
                        }, cancellationToken);
                else
                    await mDatabase.Repository<PatchEntity>()
                        .Update(patch.Entity.Id)
                        .Set(x => x.AppliedAt, DateTime.UtcNow)
                        .Apply(token: cancellationToken);
            }
            catch (Exception e)
            {
                mLogger.Error($"Patch {name} was failed", e);
                hasError = true;
                break;
            }
        }
            
        if(!hasError && unAppliedPatches.Length > 0)
            mLogger.Success("Patches successfully applied");
    }

    /// <inheritdoc />
    public Task Migrate(string category, string destination, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(category)) 
            throw new ArgumentNullException(nameof(category));
            
        if (string.IsNullOrEmpty(destination))
            throw new ArgumentNullException(nameof(destination));
            
        mLogger.Debug($"[{category}] Applying patches to {destination}...");

        var patchTypes = Types.All
            .Where(x => x.BaseType == typeof(Patch) && !x.IsAbstract)
            .ToDictionary(x => x.FullName, x => x);

        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<PatchInfo[]> GetPatches(string category = null, CancellationToken cancellationToken = default)
    {
        var patchTypes = Types.All
            .Where(x => x.BaseType == typeof(Patch) && !x.IsAbstract)
            .ToDictionary(x => x.FullName ?? string.Empty, x => x);

        var query = mDatabase.Repository<PatchEntity>().Select();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(x => x.Category == category);

        var patchesTypesNames = patchTypes.Keys;
        var patchesExisted = await query
            .Select(x => new PatchInfo
            {
                Id = x.Id,
                Name = x.Name,
                Order = x.Order,
                AppliedAt = x.AppliedAt,
                ClrType = x.ClrType,
                Category = x.Category,
                HasInCode = patchesTypesNames.Contains(x.ClrType)
            })
            .ToListAsync(cancellationToken);

        var patchesNotExisted = patchTypes
            .Where(x => patchesExisted.All(p => p.ClrType != x.Key))
            .Select(x =>
            {
                var obj = (Patch) mContainer.Resolve(x.Value);
                return new PatchInfo
                {
                    Id = null,
                    Name = x.Value.GetCustomAttribute<DescriptionAttribute>()?.Description ?? x.Value.Name,
                    Order = obj.Order,
                    AppliedAt = null,
                    ClrType = x.Key,
                    Category = obj.Category,
                    HasInCode = true
                };
            });

        if (!string.IsNullOrEmpty(category))
            patchesNotExisted = patchesNotExisted.Where(x => x.Category == category);

        patchesExisted.AddRange(patchesNotExisted.ToArray());

        return patchesExisted.OrderBy(x => x.Order).ToArray();
    }
}