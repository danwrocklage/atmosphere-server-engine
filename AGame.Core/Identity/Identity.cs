using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace AGame.Core.Identity;

public class IdentityLink
{
    public string Type { get; set; }
    
    public Guid Id { get; set; }
}

/// <summary>
/// Entity for authorization and authentication
/// </summary>
[Table("identities")]
public class Identity : IDbEntity
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Linked entity (e.g. <see cref="Account.AccountEntity"/>)
    /// </summary>
    public IdentityLink Link { get; set; }

    /// <summary>
    /// Authentication type
    /// </summary>
    public IdentityType Type { get; set; }

    /// <summary>
    /// Public identity item (login, public key etc.)
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Private identity item (password hash, private key etc.)
    /// </summary>
    public string Secret { get; set; }

    /// <summary>
    /// Global rights which grant for this <see cref="Identity"/>
    /// </summary>
    public List<string> GrandTypes { get; set; }
    
    /// <summary>
    /// Max failed tries before it will be blocked
    /// </summary>
    public int FailsAvailable { get; set; }
    
    /// <summary>
    /// Entity creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Entity last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Identity type
/// </summary>
public enum IdentityType : byte
{
    /// <summary>
    /// Basic authorization
    /// </summary>
    LoginPassword,
    
    /// <summary>
    /// OAuth
    /// </summary>
    OAuth
}