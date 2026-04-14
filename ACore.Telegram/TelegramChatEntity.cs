using System.ComponentModel.DataAnnotations.Schema;
using ACore.Abstractions.Database;

namespace ACore.Telegram;

/// <summary>
/// Entity for store telegram char IDs
/// </summary>
[Table("telegram.chat")]
internal class TelegramChatEntity : IDbEntity
{
    public Guid Id { get; set; }

    public long ChatId { get; set; }
    
    public DateTime CreatedAt { get; set; }
}