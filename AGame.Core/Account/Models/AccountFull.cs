namespace AGame.Core.Account.Models;

public class AccountFull
{
    /// <summary>
    /// Account identifier
    /// </summary>
    public Guid Id { get; set; }
        
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
        
    /// <summary>
    /// Account last active date
    /// </summary>
    public DateTime LastActive { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public int CharacterMaxCount { get; set; }

    /// <summary>
    /// Is the account activated
    /// </summary>
    public bool IsActivated { get; set; }

    /// <summary>
    /// Is account marked as deleted?
    /// </summary>
    public bool IsDeleted { get; set; }
        
    public bool IsActive { get; set; }

    /// <summary>
    /// Account creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }
}