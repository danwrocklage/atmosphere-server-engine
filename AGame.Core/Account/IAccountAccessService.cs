namespace AGame.Core.Account;

public interface IAccountAccessService
{
    Task<AccountStatus?> GetAccountStatus(Guid id);

    Task<bool?> CanAuthenticate(Guid accountId);

    Task<bool?> CanPlay(Guid accountId);

    Task UnblockAccountsByPassword();

    //Task<AccountAuthResult> Authenticate(string key, string secret);
}