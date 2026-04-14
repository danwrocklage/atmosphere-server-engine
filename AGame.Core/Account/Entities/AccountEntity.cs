using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;
using AGame.Core.ClientApp;

namespace AGame.Core.Account;

/// <summary>
/// Player account
/// </summary>
[Table("accounts")]
public class AccountEntity : IDbEntity
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
    /// Link to <see cref="AccountStatusEntity"/>
    /// </summary>
    public Guid StatusId { get; set; }
        
    /// <summary>
    /// Link to <see cref="AccountEmailSettingsEntity"/>
    /// </summary>
    public Guid EmailSubscriptionId { get; set; }
        
    /// <summary>
    /// Link to <see cref="Identity"/>
    /// </summary>
    public List<Guid> Identities { get; set; }
        
    /// <summary>
    /// Account creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Client application builds type, which account can use
    /// </summary>
    public ClientBuildType AccessedClientBuildType { get; set; }
    
    /// <summary>
    /// From where player come and register
    /// </summary>
    public RegistrationSource Source { get; set; }
    
    /// <summary>
    /// How many characters an account can have
    /// </summary>
    public byte CharacterMaxCount { get; set; }
}