using System.Collections.Concurrent;
using ACore.Abstractions.Storage;
using AGame.Actors.Avatar;
using AGame.Actors.Replication;
using AGame.Transform;
using AUtils.Math;
using Fb.Mechanics.PlayerCharacter;

namespace Fb.Frontend;

public interface IAfkTracker
{
    AfkState GetState(Guid characterActorId);
    void UpdateTime(Guid characterActorId);
    void Remove(Guid characterActorId);
}

internal class AfkTracker : IAfkTracker
{
    private readonly TimeSpan mAfkTime;
    private readonly TimeSpan mInactiveTime;
    private readonly ConcurrentDictionary<Guid, DateTime> mLastActiveTime;

    public AfkTracker()
    {
        mLastActiveTime = new ConcurrentDictionary<Guid, DateTime>();
        mAfkTime = TimeSpan.FromHours(1);
        mInactiveTime = TimeSpan.FromMinutes(10);
    }
    
    public AfkState GetState(Guid characterActorId)
    {
        if(!mLastActiveTime.TryGetValue(characterActorId, out var lastActiveTime))
            return AfkState.Active;

        var now = DateTime.UtcNow;
        if (lastActiveTime + mAfkTime < now)
            return AfkState.Afk;
        if (lastActiveTime + mInactiveTime < now)
            return AfkState.Inactive;

        return AfkState.Active;
    }

    public void UpdateTime(Guid characterActorId)
    {
        mLastActiveTime.AddOrUpdate(characterActorId, _ => DateTime.UtcNow, (_, _) => DateTime.UtcNow);
    }

    public void Remove(Guid characterActorId)
    {
        mLastActiveTime.TryRemove(characterActorId, out _);
    }
}

public enum AfkState : byte
{
    Active,
    Inactive,
    Afk
}