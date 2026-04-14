using ACore.Abstractions;
using ACore.Abstractions.Database;
using AGame.Core.Identity;
using AGame.Core.Staff;
using AUtils.IoC;
using Fb.Web.Shared;

namespace Fb.Web.Admin;

[ACore.Modules.Order(1)]
public class WebAdminModule : ACore.Modules.Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.AddWebSharedServices();
        builder.Transient<WebAdminWorker>();
    }

    [ACore.Modules.RoleAny(Cell.ADMIN_API)]
    public async Task Start(CancellationToken token = default)
    {
        Worker<WebAdminWorker>(token);

        var db = Services.Resolve<IDatabase>();
        if (!await db.Select<StaffEntity>().AnyAsync(token))
        {
            var identity = Guid.NewGuid();
            var staff = Guid.NewGuid();
            var role = Guid.NewGuid();
            await db.Repository<Identity>().Insert(new Identity
            {
                Id = identity,
                Key = "admin",
                Secret = BCrypt.Net.BCrypt.EnhancedHashPassword("admin"),
                Link = new IdentityLink
                {
                    Id = staff,
                    Type = typeof(StaffEntity).FullName
                },
                Type = IdentityType.LoginPassword,
                CreatedAt = DateTime.UtcNow,
                FailsAvailable = 5,
                UpdatedAt = DateTime.UtcNow,
                GrandTypes = new List<string> {GrandTypes.WebAdmin}
            }, token);

            await db.Repository<StaffRoleEntity>().Insert(new StaffRoleEntity
            {
                Id = role,
                Name = "Administrator",
                Scopes = new[] {"*"}
            }, token);
            
            await db.Repository<StaffEntity>().Insert(new StaffEntity
            {
                Id = staff,
                Email = "dan@staff.atmosphere",
                Name = "Dan Staff",
                CreateAt = DateTime.UtcNow,
                IsActivated = true,
                IdentityId = identity,
                IsDeleted = false,
                RoleId = role,
                AvatarUrl = ""
            }, token);
        }
    }
}