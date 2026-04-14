using System.Linq.Expressions;
using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Storage;
using AGame.Core.Account.Models;
using AGame.Core.ClientApp;
using AGame.Core.Identity;
using AGame.Core.Journal;

namespace AGame.Core.Account;

/// <inheritdoc cref="IAccountService"/>
internal class AccountService : IAccountService, IAccountAccessService
{
    private record AccountEntityWithStatus(AccountEntity Entity, AccountStatus Status);
    
    private readonly ILogger<AccountService> mLogger;
    private readonly IDatabase mDatabase;
    private readonly IRepository<AccountEntity> mAccounts;
    private readonly IJournalService mJournalService;
    private readonly IIdentityService mIdentityService;
    private readonly IStorageHash<AccountShort> mShortAccountHash;
    private readonly IStorageHash<AccountStatus?> mAccountsStatusHash;

    public AccountService(IStorage storage, IDatabase database, 
        ILogger<AccountService> logger, IJournalService journalService, 
        IIdentityService identityService)
    {
        mDatabase = database;
        mAccounts = database.Repository<AccountEntity>();
        mLogger = logger;
        mAccountsStatusHash = storage.HashOf<AccountStatus?>("account:status");
        mJournalService = journalService;
        mIdentityService = identityService;

        mShortAccountHash = storage.HashOf<AccountShort>("account:short");
    }

    public async Task<List<AccountFull>> GetAccounts(AccountFilter filter) =>
        await mDatabase.Repository<AccountEntity>().Select()
            .OrderByDescending(x => x.CreatedAt)
            .Join(x => x.StatusId, (AccountEntity x, AccountStatusEntity s) => new AccountEntityWithStatus(x, s.Status))
            .Select(MapToDomain())
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .ToListAsync();

    /// <inheritdoc />
    public async Task<AccountShort> GetShortAccountById(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentNullException(nameof(id));

        var cachedAccount = await mShortAccountHash.Get(id.ToString());
        if (cachedAccount != null) 
            return cachedAccount;
            
        var dbAccount = await mAccounts.Select()
            .Where(x => x.Id == id)
            .Select(x => new AccountShort
            {
                Name = x.Name,
                AvatarUrl = x.AvatarUrl
            })
            .FirstOrDefaultAsync();
        if (dbAccount == null) 
            return null;
        
        await mShortAccountHash.Store(id.ToString(), dbAccount);
        return dbAccount;
    }

    /// <inheritdoc />
    public async Task<AccountFull> GetAccountById(Guid id)
    {
        if(id == Guid.Empty)
            throw new ArgumentNullException(nameof(id));
            
        return await mAccounts.Select()
            .Where(x => x.Id == id)
            .Join(x => x.StatusId, (AccountEntity x, AccountStatusEntity s) => new AccountEntityWithStatus(x, s.Status))
            .Select(MapToDomain())
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public Task UpdateAccountActivity(Guid id)
    {
        return mAccounts.Update(id)
            .Set(x => x.LastActive, DateTime.UtcNow)
            .Apply();
    }

    /// <inheritdoc />
    public Task<bool> IsEmailExists(string email)
    {
        if (email == null) 
            throw new ArgumentNullException(nameof(email));
        
        return mAccounts.Select()
            .AnyAsync(x => x.Email == email);
    }

    public async Task<AccountStatus?> GetAccountStatus(Guid id)
    {
        var canAuth = await mAccountsStatusHash.Get(id.ToString());
        if (canAuth.HasValue)
            return canAuth.Value;
        
        var status = await mAccounts.Select()
            .Join(x => x.StatusId, (AccountEntity x, AccountStatusEntity s) => new
            {
                x.Id,
                s.Status
            })
            .Where(x => x.Id == id && x.Status != AccountStatus.Deleted)
            .Select(x => (AccountStatus?) x.Status)
            .FirstOrDefaultAsync();

        if (status != null)
            await mAccountsStatusHash.Store(id.ToString(), status);

        return status;
    }

    /// <inheritdoc />
    public async Task<bool?> CanAuthenticate(Guid accountId)
    {
        var status = await GetAccountStatus(accountId);
        return status.HasValue ? status.Value != AccountStatus.BlockedByPassword : null;
    }

    /// <inheritdoc />
    public async Task<bool?> CanPlay(Guid accountId)
    {
        var status = await GetAccountStatus(accountId);
        return status.HasValue ? status.Value == AccountStatus.Active : null;
    }

    public async Task UnblockAccountsByPassword()
    {
        var now = DateTime.UtcNow;
        await mDatabase.Repository<AccountStatusEntity>()
            .Update(x => x.Status == AccountStatus.BlockedByPassword && x.Until.HasValue && x.Until <= now)
            .Set(x => x.Reason, null)
            .Set(x => x.Status, AccountStatus.Active)
            .Set(x => x.Until, null)
            .Apply();
    }

    /// <inheritdoc />
    public async Task<string> CreateNewAccount(AccountCreate accountCreate)
    {
        mLogger.Info("Creating new account");
        var activationToken = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());
        var accountId = Guid.NewGuid();
        var accountStatusId = Guid.NewGuid();
        var emailSettingsId = Guid.NewGuid();
  
        await mDatabase.Repository<AccountEmailSettingsEntity>()
            .Insert(new AccountEmailSettingsEntity
            {
                Id = emailSettingsId,
                AccountId = accountId,
                SendPrimaryEmails = true,
                SendNewsEmails = accountCreate.EmailSubscription
            });

        await mDatabase.Repository<AccountStatusEntity>()
            .Insert(new AccountStatusEntity
            {
                Id = accountStatusId,
                ActivationWay = AccountActivationWay.Email,
                ActivationToken = activationToken,
                Status = AccountStatus.NotActivated,
                Reason = null,
                Comment = null,
                Until = null
            });
            
        await mAccounts.Insert(new AccountEntity
        {
            Name = accountCreate.Name,
            Email = accountCreate.Email,
            Id = accountId,
            CreatedAt = DateTime.UtcNow,
            LastActive = DateTime.UtcNow,
            AccessedClientBuildType = ClientBuildType.Public,
            Identities = new List<Guid> { accountCreate.IdentityId },
            EmailSubscriptionId = emailSettingsId,
            CharacterMaxCount = 3,
            Source = accountCreate.Source,
            AvatarUrl = null,
            StatusId = accountStatusId
        });

        await mIdentityService.Link(accountCreate.IdentityId, accountId, typeof(AccountEntity).FullName);

        await mJournalService.Write<AccountEntity>(accountId, "Account was created");

        return activationToken;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateAccount(string activationToken)
    {
        var account = await mDatabase.Select<AccountStatusEntity>()
            .Where(x => x.Status == AccountStatus.NotActivated && x.ActivationToken == activationToken)
            .Select(x => new
            {
                x.Id, 
                IsActivated = string.IsNullOrEmpty(x.ActivationToken)
            })
            .FirstOrDefaultAsync();
        if (account == null || account.IsActivated)
            return false;

        mLogger.Info($"Activate {account.Id.ToString()} account");
        await mDatabase.Repository<AccountStatusEntity>()
            .Update(account.Id)
            .Set(x => x.ActivationToken, null)
            .Set(x => x.Status, AccountStatus.Active)
            .Apply();
            
        await mJournalService.Write<AccountEntity>(account.Id, "Account was activated");
        await mAccountsStatusHash.Delete(account.Id.ToString());
        
        return true;
    }

    /// <inheritdoc />
    public async Task ChangeStatus(AccountChangeStatus changeStatus)
    {
        var account = await mAccounts.Select()
            .Join(x => x.StatusId, (AccountEntity x, AccountStatusEntity s) => new
            {
                AccountId = x.Id,
                Status = s
            })
            .Where(x => x.AccountId == changeStatus.AccountId)
            .Select(x => x.Status)
            .FirstOrDefaultAsync();
        
        if(account.Status == changeStatus.Status)
            return;

        account.Status = changeStatus.Status;
        account.Reason = changeStatus.Reason;
        account.Until = changeStatus.Until;
        await mDatabase.Repository<AccountStatusEntity>().Update(account);

        await mAccountsStatusHash.Delete(account.Id.ToString());

        await mJournalService.Write<AccountEntity>(changeStatus.AccountId, $"Account was changed status to {account.Status}");
    }

    public async Task UpdateAccount(AccountUpdate accountUpdate)
    {
        if (accountUpdate == null) 
            throw new ArgumentNullException(nameof(accountUpdate));

        var account = await mAccounts.Select()
            .FirstOrDefaultAsync(x => x.Id == accountUpdate.AccountId);
        
        if(account == null)
            return;

        // TODO: validate email
        if (!string.IsNullOrEmpty(accountUpdate.Email))
            account.Email = accountUpdate.Email;

        if (!string.IsNullOrEmpty(accountUpdate.Name))
            account.Name = accountUpdate.Name;
        
        //TODO: Validate Avatar URL
        if (!string.IsNullOrEmpty(accountUpdate.AvatarUrl))
            account.AvatarUrl = accountUpdate.AvatarUrl;

        await mAccounts.Update(account);
    }

    /// <inheritdoc />
    public async Task SetComment(Guid accountId, string comment)
    {
        if (comment == null) 
            throw new ArgumentNullException(nameof(comment));

        await mDatabase.Repository<AccountStatusEntity>()
            .Update(accountId)
            .Set(x => x.Comment, comment)
            .Apply();
    }

    private static Expression<Func<AccountEntityWithStatus, AccountFull>> MapToDomain() =>
        x => new AccountFull
        {
            Id = x.Entity.Id,
            Name = x.Entity.Name,
            CreatedAt = x.Entity.CreatedAt,
            LastActive = x.Entity.LastActive,
            Email = x.Entity.Email,
            AvatarUrl = x.Entity.AvatarUrl,
            CharacterMaxCount = x.Entity.CharacterMaxCount,
            IsActivated = x.Status != AccountStatus.NotActivated,
            IsActive = x.Status == AccountStatus.Active,
            IsDeleted = x.Status == AccountStatus.Deleted
        };
}