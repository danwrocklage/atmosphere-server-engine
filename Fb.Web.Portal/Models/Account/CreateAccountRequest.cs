namespace Fb.Web.Portal.Models.Account;

public class CreateAccountRequest
{
    public string Name { get; set; }
        
    public string Email { get; set; }
        
    public string Login { get; set; }
        
    public string Password { get; set; }
        
    public bool EmailSubscription { get; set; }
}