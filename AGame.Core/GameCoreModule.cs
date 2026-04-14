using AGame.Core.Account;
using AGame.Core.ClientApp;
using AGame.Core.Feed;
using AGame.Core.Forum;
using AGame.Core.Identity;
using AGame.Core.Journal;
using AGame.Core.Staff;
using AUtils.IoC;

namespace AGame.Core;

public class GameCoreModule : ACore.Modules.Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Singleton<JwtService, IJwtService>();
        builder.Singleton<IdentityService, IIdentityService>();

        builder.Singleton<JournalService, IJournalService>();
        builder.Singleton<ClientBuildService, IClientBuildService>();

        builder.Singleton<AccountService, IAccountService, IAccountAccessService>();
        builder.Singleton<StaffService, IStaffService>();
        
        builder.Singleton<FeedService, IFeedService>();
        builder.Singleton<ForumService, IForumService>();
    }
}