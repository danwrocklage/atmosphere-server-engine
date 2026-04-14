namespace AGame.Core.Account;

public class AccountChangeStatus
{
    public Guid AccountId { get; set; }
    
    public AccountStatus Status { get; set; }
    
    public string Reason { get; set; }
    
    public DateTime? Until { get; set; }
}