using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace AGame.Core.Staff;

[Table("staffs.roles")]
public class StaffRoleEntity : IDbEntity
{
    public Guid Id { get; set; }
        
    public string Name { get; set; }
        
    public string[] Scopes { get; set; }
}