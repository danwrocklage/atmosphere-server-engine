namespace AGame.Core.Account.Models;

public class AccountCreate
{
    public string Name { get; set; }
        
    public string Email { get; set; }
    
    public Guid IdentityId { get; set; }
        
    public bool EmailSubscription { get; set; }
    
    public RegistrationSource Source { get; set; }
}