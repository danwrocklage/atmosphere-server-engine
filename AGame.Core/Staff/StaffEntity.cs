using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace AGame.Core.Staff;

/// <summary>
/// Сотрудник системы (администратор, менеджер и пр.)
/// </summary>
[Table("staffs")]
public class StaffEntity : IDbEntity
{
    public Guid Id { get; set; }
        
    /// <summary>
    /// Имя
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Почта
    /// </summary>
    public string Email { get; set; }
        
    /// <summary>
    /// Роль
    /// </summary>
    public Guid RoleId { get; set; }
        
    /// <summary>
    /// Адрес аватарки
    /// </summary>
    public string AvatarUrl { get; set; }
        
    /// <summary>
    /// Сущности авторизации
    /// </summary>
    public Guid IdentityId { get; set; }
        
    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreateAt { get; set; }
        
    public bool IsDeleted { get; set; }
        
    public bool IsActivated { get; set; }
}