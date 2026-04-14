using System.Diagnostics.CodeAnalysis;
using AGame.Core.Account.Models;

namespace AGame.Core.Account;

public interface IAccountService
{
    /// <summary>
    /// Get list if accounts
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    Task<List<AccountFull>> GetAccounts(AccountFilter filter); 

    /// <summary>
    /// Get account name and avatar. Available for all authenticated users
    /// </summary>
    Task<AccountShort> GetShortAccountById(Guid id);
    
    /// <summary>
    /// Get full account information. Available for self user and administration
    /// </summary>
    Task<AccountFull> GetAccountById(Guid id);

    /// <summary>
    /// Update timestamp of last activity
    /// </summary>
    Task UpdateAccountActivity(Guid id);

    /// <summary>
    /// Return true, if specified email is already used
    /// </summary>
    Task<bool> IsEmailExists(string email);

    /// <summary>
    /// Create new account
    /// </summary>
    Task<string> CreateNewAccount(AccountCreate accountCreate);

    /// <summary>
    /// Activate just created account
    /// </summary>
    Task<bool> ActivateAccount(string activationToken);

    /// <summary>
    /// Change account status
    /// </summary>
    Task ChangeStatus(AccountChangeStatus changeStatus);

    /// <summary>
    /// Update account
    /// </summary>
    Task UpdateAccount(AccountUpdate accountUpdate);

    /// <summary>
    /// Apply comment to account
    /// </summary>
    Task SetComment(Guid accountId, [NotNull] string comment);
}