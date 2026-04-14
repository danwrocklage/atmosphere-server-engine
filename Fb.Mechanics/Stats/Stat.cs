namespace Fb.Mechanics.Stats;

public struct Stat
{
    public static readonly Stat Empty = new(default, false); 
    
    private readonly Dictionary<string, (int, DateTime)> mModificators;
    private int mSource;
    
    public Stat(int source, bool isModifiable)
    {
        mSource = source;
        Value = source;
        mModificators = isModifiable ? new() : null;
    }

    public int Source
    {
        get => mSource;
        set
        {
            mSource = value;
            UpdateValue();
        } 
    }
    
    public int Value { get; private set; }

    public void AddModificator(string name, StatModificator modificator)
    {
        if(mModificators == null)
            throw new MechanicException("Stat isn't modifiable");
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        var expiredAt = modificator.Duration == TimeSpan.Zero
            ? default
            : DateTime.UtcNow + modificator.Duration;
        var value = (modificator.Value, expiredAt);
        if (mModificators.ContainsKey(name))
            mModificators[name] = value;
        else
            mModificators.Add(name, value);

        UpdateValue();
    }

    public void RemoveExpiredModificators()
    {
        if(mModificators.Count == 0)
            return;
        
        var now = DateTime.UtcNow;
        foreach (var key in mModificators.Keys.ToArray())
        {
            var expiredAt = mModificators[key].Item2;
            if (expiredAt != default && expiredAt <= now)
                mModificators.Remove(key);
        }
    }

    private void UpdateValue()
    {
        if (mModificators == null)
        {
            Value = mSource;
            return;
        }
        
        var modificatorsDelta = 0;
        foreach (var (modificator, _) in mModificators.Values)
        {
            modificatorsDelta += modificator;
        }

        Value = mSource + modificatorsDelta;
    }
}