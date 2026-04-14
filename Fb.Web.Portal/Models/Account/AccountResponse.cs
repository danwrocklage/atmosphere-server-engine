namespace Fb.Web.Portal.Models.Account;

public class AccountResponse : AccountShortResponse
{
    public string Email { get; set; }
        
    public DateTime LastActive { get; set; }
        
    public DateTime CreateAt { get; set; }
}