using System.Reflection;

namespace Fb.Mechanics.Stats;

public static class StatTypeExtensions
{
    private static readonly Dictionary<StatType, bool> sModifiables = typeof(StatType).GetMembers()
        .ToDictionary(x => Enum.Parse<StatType>(x.Name),
            x => x.GetCustomAttribute<IsNotModifiableAttribute>() == null);

    public static bool IsModifiable(this StatType statType) => sModifiables[statType];
}