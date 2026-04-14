namespace AGame.Core.Staff.Models;

public class StaffItem
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
    public StaffItemRole Role { get; set; }
        
    /// <summary>
    /// Адрес аватарки
    /// </summary>
    public string AvatarUrl { get; set; }

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreateAt { get; set; }
        
    public bool IsDeleted { get; set; }
        
    public bool IsActivated { get; set; }
}

public class StaffItemRole
{
    public Guid Id { get; set; }
        
    public string Name { get; set; }
}