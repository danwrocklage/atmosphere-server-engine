using System.Diagnostics.CodeAnalysis;

namespace Fb.Mechanics.Stats;

/// <summary>
/// Тип показателя
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum StatType : byte
{
    // ---------- PHYSICAL STATS ----------

    /// <summary>
    /// Сила
    /// </summary>
    Strength,

    /// <summary>
    /// Ловкость
    /// </summary>
    Agility,

    /// <summary>
    /// Здоровье
    /// </summary>
    HP,

    /// <summary>
    /// Восстановление здоровья
    /// </summary>
    HPRegen,

    /// <summary>
    /// Выносливость
    /// </summary>
    Stamina,

    /// <summary>
    /// Восстановление выносливости
    /// </summary>
    StaminaRegen,

    /// <summary>
    /// Кислород
    /// </summary>
    Oxygen,

    /// <summary>
    /// Урон
    /// </summary>
    Damage,

    /// <summary>
    /// Критический урон
    /// </summary>
    CriticalDamage,

    /// <summary>
    /// Шанс критического урона
    /// </summary>
    CriticalDamageChance,

    /// <summary>
    /// Защита
    /// </summary>
    Defence,

    /// <summary>
    /// Уклонение
    /// </summary>
    Evasion,

    /// <summary>
    /// Шанс уклонения
    /// </summary>
    EvasionChance,

    /// <summary>
    /// Скорость передвижения
    /// </summary>
    MovementSpeed,

    /// <summary>
    /// Скорость атаки
    /// </summary>
    AttackSpeed,

    // ---------- META STATS ----------

    /// <summary>
    /// Опыт
    /// </summary>
    [IsNotModifiable]
    Experience,

    /// <summary>
    /// Уровень
    /// </summary>
    [IsNotModifiable]
    Level,

    /// <summary>
    /// Удача
    /// </summary>
    [IsNotModifiable]
    Luck,

    /// <summary>
    /// Репутация
    /// </summary>
    Reputation,

    /// <summary>
    /// Шанс
    /// </summary>
    [IsNotModifiable]
    Chance,

    /// <summary>
    /// Интеллект
    /// </summary>
    Intelligence,

    // ---------- MORAL STATS ----------

    /// <summary>
    /// Ответственность
    /// </summary>
    Responsibility,

    /// <summary>
    /// Доброта
    /// </summary>
    Kindness,

    /// <summary>
    /// Честность
    /// </summary>
    [IsNotModifiable]
    Honesty,

    /// <summary>
    /// Лукавость
    /// </summary>
    Crafty,

    /// <summary>
    /// Зло
    /// </summary>
    Evil,

    /// <summary>
    /// Эгоизм
    /// </summary>
    [IsNotModifiable]
    Selfishness
}