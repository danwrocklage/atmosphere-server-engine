using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace AGame.Core.ClientApp;

[Table("client.app")]
public class ClientBuildEntity : IDbEntity
{
    public Guid Id { get; set; }
    
    public string Version { get; set; }
    
    public ClientBuildType BuildType { get; set; }
    
    public ClientType Type { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

public enum ClientType : byte
{
    UnrealEngine,
}