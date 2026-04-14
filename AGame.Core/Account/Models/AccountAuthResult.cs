namespace AGame.Core.Account;

public class AccountAuthResult
{
    public string AccessToken { get; set; }
    
    public string RefreshToken { get; set; }
    
    public DateTime ExpiredAt { get; set; }
}