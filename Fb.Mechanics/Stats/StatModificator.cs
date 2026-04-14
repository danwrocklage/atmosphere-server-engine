namespace Fb.Mechanics.Stats;

public readonly struct StatModificator
{
    public int Value { get; init; }
    
    public TimeSpan Duration { get; init; }
}