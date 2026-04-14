using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace Fb.Mechanics.Guild;

[Table("guilds")]
public class GuildEntity : IDbEntity
{
    public Guid Id { get; set; }
    
    public string Name { get; set; }
    
    public string ExternalChatId { get; set; }
    
    public Guid OwnerId { get; set; }
    
    public GuildMembersEntity Members { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

public class GuildMembersEntity
{
    public Dictionary<Guid, GuildMemberPermissions> Members { get; set; }
    
    public int MaxCount { get; set; }
}

public enum GuildMemberPermissions : byte
{
    Owner,
    Regular,
    CanInvite
}