using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace AGame.Core.Account;

[Table("accounts.status")]
public class AccountStatusEntity : IDbEntity
{
    /// <summary>
    /// Entity id. Same as <see cref="AccountEntity.Id"/>
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Status of account
    /// </summary>
    public AccountStatus Status { get; set; }
    
    /// <summary>
    /// Activation way
    /// </summary>
    public AccountActivationWay ActivationWay { get; set; }
    
    /// <summary>
    /// Activation code for confirm player person
    /// </summary>
    public string ActivationToken { get; set; }
    
    /// <summary>
    /// Visible for player account blocking reason
    /// </summary>
    public string Reason { get; set; }
    
    /// <summary>
    /// Note by staff about this account (used for internal blocking reason)
    /// </summary>
    public string Comment { get; set; }
    
    /// <summary>
    /// Due date when account will become active again
    /// </summary>
    public DateTime? Until { get; set; }
}