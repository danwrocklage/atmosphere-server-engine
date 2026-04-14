namespace AGame.Core.Account.Models;

public class AccountUpdate
{
    /// <summary>
    /// Account identifier
    /// </summary>
    public Guid AccountId { get; set; }
    
    /// <summary>
    /// Account name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Account email
    /// </summary>
    public string Email { get; set; }
        
    /// <summary>
    /// Avatar image url
    /// </summary>
    public string AvatarUrl { get; set; }
}