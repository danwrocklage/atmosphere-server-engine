using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace AGame.Core.Account;

[Table("accounts.email")]
public class AccountEmailSettingsEntity : IDbEntity
{
    public Guid Id { get; set; }
        
    public Guid AccountId { get; set; }
        
    public bool SendPrimaryEmails { get; set; }
    public bool SendNewsEmails { get; set; }
}