namespace AGame.Core.Account;

/// <summary>
/// Account statuses
/// </summary>
public enum AccountStatus : byte
{
    /// <summary>
    /// All is good.
    /// Auth: Y
    /// Play: Y
    /// </summary>
    Active,
    
    /// <summary>
    /// Account just now was created. Activation by email required.
    /// Auth: Y
    /// Play: N
    /// </summary>
    NotActivated,
    
    /// <summary>
    /// Account was temporarily blocked due to exceeding attempts count.
    /// Auth: N
    /// Play: N
    /// </summary>
    BlockedByPassword,
    
    /// <summary>
    /// Account was blocked by administrator due to a fact that the player violated game (or service) rules.
    /// Auth: Y
    /// Play: N
    /// </summary>
    BlockedByViolation,
    
    /// <summary>
    /// Account was blocked forever.
    /// Auth: N
    /// Play: N
    /// </summary>
    Deleted
}