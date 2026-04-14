using AGame.Actors;

namespace Fb.Mechanics.Stats;

public class StatComponent : ActorComponent
{
    private Dictionary<StatType, Stat> mStats = new();
    private TimeSpan mElapsed = TimeSpan.Zero;

    public Stat Get(StatType type) => mStats.TryGetValue(type, out var stat) ? stat : Stat.Empty;

    public bool Contains(StatType type) => mStats.ContainsKey(type);

    public void Load(IReadOnlyDictionary<StatType, int> stats)
    {
        mStats = stats.ToDictionary(
            x => x.Key, 
            x => new Stat(x.Value, x.Key.IsModifiable()));
    }

    public IReadOnlyDictionary<StatType, int> Store() => mStats
        .ToDictionary(x => x.Key, x => x.Value.Source);

    protected override void Tick(TimeSpan delta)
    {
        mElapsed += delta;

        if (mElapsed.Milliseconds >= 500)
        {
            foreach (var stat in mStats.Values)
            {
                stat.RemoveExpiredModificators();
            }
            mElapsed -= TimeSpan.FromMilliseconds(500);
        }
    }
}