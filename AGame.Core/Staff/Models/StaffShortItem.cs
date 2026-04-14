namespace AGame.Core.Staff.Models;

public class StaffShortItem
{
    public Guid Id { get; set; }
        
    /// <summary>
    /// Имя
    /// </summary>
    public string Name { get; set; }
        
    /// <summary>
    /// Роль
    /// </summary>
    public string Role { get; set; }
        
    /// <summary>
    /// Адрес аватарки
    /// </summary>
    public string AvatarUrl { get; set; }
        
    public bool IsDeleted { get; set; }
        
    public bool IsActivated { get; set; }
}