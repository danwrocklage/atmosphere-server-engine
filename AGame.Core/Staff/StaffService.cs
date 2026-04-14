using ACore.Abstractions;
using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Rpc;
using ACore.Abstractions.Storage;
using AGame.Core.Identity;
using AGame.Core.Journal;
using AGame.Core.Staff.Models;

namespace AGame.Core.Staff;

internal class StaffService : IStaffService
{
    private readonly ILogger<StaffService> mLogger;
    private readonly IDatabase mDatabase;
    private readonly IStorage mStorage;
    private readonly IJournalService mJournalService;
    private readonly IIdentityService mIdentityService;
    private readonly IRpc mRpc;

    public StaffService(ILogger<StaffService> logger, 
        IDatabase database, IJournalService journalService, 
        IStorage storage, IIdentityService identityService, IRpc rpc)
    {
        mLogger = logger;
        mDatabase = database;
        mJournalService = journalService;
        mStorage = storage;
        mIdentityService = identityService;
        mRpc = rpc;
    }

    public Task<List<StaffShortItem>> GetStaffs(StaffFilter filter)
    {
        if (filter == null) 
            throw new ArgumentNullException(nameof(filter));
            
        return mDatabase.Repository<StaffEntity>().Select()
            .Join<StaffRoleEntity, StaffShortItem>(x => x.RoleId, (x, role) => new StaffShortItem
            {
                Id = x.Id,
                Name = x.Name,
                Role = role.Name,
                AvatarUrl = x.AvatarUrl,
                IsActivated = x.IsActivated,
                IsDeleted = x.IsDeleted
            })
            .Skip((filter.Page ?? default) * filter.Size)
            .Take(filter.Size)
            .ToListAsync();
    }

    public async Task<StaffShortItem> GetStaffShort(Guid id)
    {
        var staffCache = mStorage.HashOf<StaffShortItem>(CacheTopic.Staff.Shorts);
        var staffShort = await staffCache.Get(id.ToString());
        if (staffShort != null)
            return staffShort;

        var staffShortFromDb = await mDatabase.Repository<StaffEntity>().Select()
            .Where(x => x.Id == id)
            .Join<StaffRoleEntity, StaffShortItem>(x => x.RoleId, (x, role) => new StaffShortItem
            {
                Id = x.Id,
                Name = x.Name,
                Role = role.Name,
                AvatarUrl = x.AvatarUrl,
                IsActivated = x.IsActivated,
                IsDeleted = x.IsDeleted
            })
            .FirstOrDefaultAsync();

        if (staffShortFromDb != null)
            await staffCache.Store(id.ToString(), staffShortFromDb);

        return staffShortFromDb;
    }

    public Task<StaffItem> GetStaff(Guid id) => 
        mDatabase.Repository<StaffEntity>()
            .Select()
            .Where(x => x.Id == id)
            .Join<StaffRoleEntity, StaffItem>(x => x.RoleId, (x, role) => new StaffItem
            {
                Id = x.Id,
                Email = x.Email,
                Name = x.Name,
                Role = new StaffItemRole {Id = x.RoleId, Name = role.Name},
                AvatarUrl = x.AvatarUrl,
                CreateAt = x.CreateAt,
                IsActivated = x.IsActivated,
                IsDeleted = x.IsDeleted
            })
            .FirstOrDefaultAsync();

    public async Task<Guid?> Create(CreateStaff model)
    {
        if (!await Validate(model))
        {
            mLogger.Warn("Validation failed to create new staff");
            return null;
        }
        
        mLogger.Info($"Create new staff {model.Name}");
        
        var staffId = Guid.NewGuid();
        await mDatabase.Repository<StaffEntity>()
            .Insert(new StaffEntity
            {
                Id = staffId,
                Email = model.Email,
                Name = model.Name,
                AvatarUrl = model.AvatarUrl,
                CreateAt = DateTime.UtcNow,
                RoleId = model.RoleId,
                IdentityId = model.IdentityId,
                IsActivated = false,
                IsDeleted = false
            });

        await mIdentityService.Link(model.IdentityId, staffId, typeof(StaffEntity).FullName);
        await mJournalService.Write<StaffEntity>(staffId, "Staff was created");
        await mRpc.Call(new GlobalNotificationEvent
            {Message = $"[{nameof(StaffEntity)}] New staff {model.Name} with identity {model.IdentityId}"});
        return staffId;
    }

    public async Task Edit(Guid staffId, EditStaff model)
    {
        if (!await Validate(model))
        {
            mLogger.Error($"Validation failed to update staff {staffId}");
            return;
        }
            
        mLogger.Info($"Update staff {staffId}");

        await mStorage
            .HashOf<StaffShortItem>(CacheTopic.Staff.Shorts)
            .Delete(staffId.ToString());
            
        await mDatabase.Repository<StaffEntity>().Update(staffId)
            .Set(x => x.Name, model.Name)
            .Set(x => x.Email, model.Email)
            .Set(x => x.RoleId, model.RoleId)
            .Set(x => x.AvatarUrl, model.AvatarUrl)
            .Apply();
            
        await mJournalService.Write<StaffEntity>(staffId, "Staff was updated");
        
        await mRpc.Call(new GlobalNotificationEvent
            {Message = $"[{nameof(StaffEntity)}] Staff {staffId} was updated"});
    }

    public async Task Delete(Guid staffId, Guid staffInitiatorId)
    {
        if (!await IsStaffExists(staffInitiatorId))
        {
            mLogger.Warn($"Can't delete staff {staffId}. Initiator staff {staffInitiatorId} was not found");
            return;
        }
            
        if (!await IsStaffExists(staffId))
        {
            mLogger.Warn($"Can't delete staff. Staff {staffId} was not found");
            return;
        }

        mLogger.Info($"Delete staff {staffId} by staff {staffInitiatorId}");
        await mDatabase.Repository<StaffEntity>()
            .Update(staffId)
            .Set(x => x.IsDeleted, true)
            .Apply();

        await mJournalService.Write($"Staff {staffId} was deleted", nameof(StaffEntity),
            JournalLink.Create<StaffEntity>(staffId),
            JournalLink.Create<StaffEntity>(staffInitiatorId));
    }

    public async Task Activate(Guid staffId, Guid staffInitiatorId)
    {
        if (!await IsStaffExists(staffInitiatorId))
        {
            mLogger.Warn($"Can't activate staff {staffId}. Initiator staff {staffInitiatorId} was not found");
            return;
        }
            
        if (!await IsStaffExists(staffId))
        {
            mLogger.Warn($"Can't activate staff. Staff {staffId} was not found");
            return;
        }

        mLogger.Info($"Activate staff {staffId} by staff {staffInitiatorId}");
        await mDatabase.Repository<StaffEntity>()
            .Update(staffId)
            .Set(x => x.IsActivated, true)
            .Apply();

        await mJournalService.Write($"Staff {staffId} was activated", nameof(StaffEntity),
            JournalLink.Create<StaffEntity>(staffId),
            JournalLink.Create<StaffEntity>(staffInitiatorId));
        
        await mRpc.Call(new GlobalNotificationEvent
            {Message = $"[{nameof(StaffEntity)}] Staff {staffId} was activated by {staffInitiatorId}"});
    }
    
    public async Task Deactivate(Guid staffId)
    {
        if (!await IsStaffExists(staffId))
        {
            mLogger.Warn($"Can't activate staff. Staff {staffId} was not found");
            return;
        }

        mLogger.Info($"Deactivate staff {staffId}");
        await mDatabase.Repository<StaffEntity>()
            .Update(staffId)
            .Set(x => x.IsActivated, false)
            .Apply();

        await mJournalService.Write($"Staff {staffId} was deactivated", nameof(StaffEntity),
            JournalLink.Create<StaffEntity>(staffId));
    }

    public async Task<bool?> CanBeAuthenticated(Guid staffId)
    {
        var staff = await mDatabase.Select<StaffEntity>()
            .Where(x => x.Id == staffId && !x.IsDeleted)
            .Select(x => new { Can = x.IsActivated && !x.IsDeleted })
            .FirstOrDefaultAsync();

        return staff?.Can;
    }

    public async Task<string[]> GetRoleScopes(Guid roleId)
    {
        var scopesCache = mStorage.HashOf<string[]>(CacheTopic.Staff.RoleScopes);
        var scopes = await scopesCache.Get(roleId.ToString());
        if (scopes != null)
            return scopes;

        var scopesFromDb = await mDatabase.Repository<StaffRoleEntity>().Select()
            .Where(x => x.Id == roleId)
            .Select(x => x.Scopes)
            .FirstOrDefaultAsync();

        if (scopesFromDb != null)
            await scopesCache.Store(roleId.ToString(), scopesFromDb);

        return scopesFromDb;
    }

    public async Task<string[]> GetStaffRoleScopes(Guid staffId) =>
        await mDatabase.Select<StaffEntity>()
            .Join<StaffRoleEntity, string[]>(x => x.RoleId, (_, x) => x.Scopes)
            .FirstOrDefaultAsync();

    public Task<List<StaffRoleItem>> GetRoles(StaffRoleFilter filter)
    {
        if (filter == null) 
            throw new ArgumentNullException(nameof(filter));

        return mDatabase.Repository<StaffRoleEntity>().Select()
            .Select(x => new StaffRoleItem
            {
                Id = x.Id,
                Name = x.Name,
                Scopes = x.Scopes
            })
            .Skip((filter.Page ?? default) * filter.Size)
            .Take(filter.Size)
            .ToListAsync();
    }

    public async Task<Guid?> CreateRole(string name, string[] scopes, Guid staffInitiatorId)
    {
        if (!await IsStaffExists(staffInitiatorId))
        {
            mLogger.Warn($"Can't create role with SCOPES:{string.Join(',', scopes)} and NAME:{name}. Staff {staffInitiatorId} was not found");
            return null;
        }

        if (string.IsNullOrEmpty(name) || scopes is not {Length: > 0})
        {
            mLogger.Debug($"Can't create role with CODE:{scopes} and NAME:{name}. Invalid data");
            return null;
        }
            
        mLogger.Info($"Create role with CODE:{scopes} and NAME:{name} by staff {staffInitiatorId}");
        var roleId = Guid.NewGuid();
        await mDatabase.Repository<StaffRoleEntity>()
            .Insert(new StaffRoleEntity
            {
                Id = roleId,
                Scopes = scopes,
                Name = name
            });

        await mJournalService.Write($"Role {name} was created", nameof(StaffRoleEntity),
            JournalLink.Create<StaffRoleEntity>(roleId),
            JournalLink.Create<StaffEntity>(staffInitiatorId));

        return roleId;
    }

    public async Task EditRole(Guid roleId, string name, string[] scopes, Guid staffInitiatorId)
    {
        if (!await IsStaffExists(staffInitiatorId))
        {
            mLogger.Warn($"Can't update role {roleId} with SCOPES:{string.Join(',', scopes)} and NAME:{name}. Staff {staffInitiatorId} was not found");
            return;
        }
            
        if(!await mDatabase.Repository<StaffRoleEntity>().Select().AnyAsync(x => x.Id == roleId))
        {
            mLogger.Warn($"Can't update role {roleId} with SCOPES:{string.Join(',', scopes)} and NAME:{name}. Role was not found");
            return;
        }

        mLogger.Info($"Updating role {roleId} with SCOPES:{string.Join(',', scopes)} and NAME:{name} by staff {staffInitiatorId}");
            
        await mStorage
            .HashOf<string>(CacheTopic.Staff.RoleScopes)
            .Delete(roleId.ToString());
            
        await mDatabase.Repository<StaffRoleEntity>().Update(roleId)
            .Set(x => x.Scopes, scopes)
            .Set(x => x.Name, name)
            .Apply();

        await mJournalService.Write($"Role {roleId} was updated", nameof(StaffRoleEntity),
            JournalLink.Create<StaffRoleEntity>(roleId),
            JournalLink.Create<StaffEntity>(staffInitiatorId));
    }

    private async Task<bool> IsStaffExists(Guid staffId) =>
        await mDatabase.Repository<StaffEntity>().Select().AnyAsync(x => x.Id == staffId);

    private async Task<bool> Validate(EditStaff model) =>
        !string.IsNullOrEmpty(model.Name) &&
        !string.IsNullOrEmpty(model.Email) &&
        await mDatabase.Repository<StaffRoleEntity>()
            .Select().AnyAsync(x => x.Id == model.RoleId);
}