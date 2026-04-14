using ACore.Abstractions.Rpc;
using AGame.Actors;
using AGame.Actors.Persistence;
using AGame.Time.Events;

namespace AGame.Time;

/// <summary>
/// Actor for ticking world game time
/// </summary>
public class GameTimeActor : Actor
{
    private readonly IRpc mRpc;
    [Persistence] private GameTime mGameTime;
    [Persistence] private double mElapsedMs;

    public GameTimeActor(IRpc rpc)
    {
        mRpc = rpc;
        mGameTime = new GameTime
        {
            Year = 1886,
            Season = Season.Fall,
            Day = 4,
            Hour = 12,
            Minutes = 0
        };
        mElapsedMs = 0;
        TickingMode = TickingMode.ActorTickingOnly;
    }

    // RPC
    public GameTime Now() => mGameTime;

    protected override void OnTick(TimeSpan delta)
    {
        mElapsedMs += delta.TotalMilliseconds * GameTime.WORLD_SPEED;

        if (mElapsedMs < 60000) 
            return;
            
        UpdateGameTime();
        mElapsedMs -= 60000;
    }

    private void UpdateGameTime()
    {
        if (mGameTime.Minutes != GameTime.MINUTES_IN_HOUR - 1)
        {
            mGameTime.Minutes++;
            return;
        }
            
        mGameTime.Minutes = 0;
        if (mGameTime.Hour != GameTime.HOURS_IN_DAY - 1)
        {
            mGameTime.Hour++;
            mRpc.Call(new TimeChangedEvent {TimeOfDay = GetTimeOfDay()});
            return;
        }
            
        mGameTime.Hour = 0;
        if (mGameTime.Day != GameTime.DAYS_IN_SEASON)
        {
            mGameTime.Day++;
            return;
        }
            
        mGameTime.Day = 1;
            
        if ((byte) mGameTime.Season != GameTime.SeasonsCount - 1)
        {
            mGameTime.Season++;
            mRpc.Call(new SeasonChangedEvent {Season = mGameTime.Season});
            return;
        }
        mGameTime.Season = 0;
        mGameTime.Year++;
        mRpc.Call(new YearChangedEvent {Year = mGameTime.Year});
    }

    private TimeOfDay GetTimeOfDay() =>
        mGameTime.Hour switch
        {
            >= 7 and < 10 => TimeOfDay.Morning,
            >= 10 and < 19 => TimeOfDay.Day,
            >= 19 and < 22 => TimeOfDay.Evening,
            >= 22 or < 7 => TimeOfDay.Night
        };
}