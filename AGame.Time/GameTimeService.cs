using ACore.Abstractions.Rpc;
using AGame.Actors.Avatar;
using AGame.Time.Events;

namespace AGame.Time;

/// <summary>
/// Service for getting global world game time
/// </summary>
public interface IGameTimeService
{
    /// <summary>
    /// Get current game time
    /// </summary>
    Task<GameTime> Now();
}

/// <inheritdoc cref="IGameTimeService" />
internal class GameTimeService : IGameTimeService, IRpcHandler<SeasonChangedEvent>, IRpcHandler<TimeChangedEvent>, IRpcHandler<YearChangedEvent>
{
    /// <summary>
    /// <see cref="GameTimeActor"/> global actor id
    /// </summary>
    internal static Guid ActorIdSingleton => new("AC3FA04C-8550-47B1-ABBF-FCD47A6E0F8A");
    
    private readonly AvatarContext mAvatarContext;

    public GameTimeService(AvatarContext avatarContext)
    {
        mAvatarContext = avatarContext;
    }
    
    /// <inheritdoc />
    public async Task<GameTime> Now()
    {
        var avatar = await mAvatarContext.Get<GameTimeActor>(ActorIdSingleton);
        return avatar.IsEmpty ? GameTime.Empty : await avatar.Rpc(x => x.Now());
    }

    /// <summary>
    /// When time of day was changed (morning, day etc.)
    /// </summary>
    public event Action<TimeOfDay> TimeChanged;
    
    /// <summary>
    /// When season of year was changed (spring, summer etc.)
    /// </summary>
    public event Action<Season> SeasonChanged;
    
    /// <summary>
    /// When year was changed
    /// </summary>
    public event Action<uint> YearChanged;

    public Task Handle(IRpcContext<SeasonChangedEvent> context, CancellationToken token = default)
    {
        SeasonChanged?.Invoke(context.Message.Season);
        return Task.CompletedTask;
    }

    public Task Handle(IRpcContext<TimeChangedEvent> context, CancellationToken token = default)
    {
        TimeChanged?.Invoke(context.Message.TimeOfDay);
        return Task.CompletedTask;
    }
    
    public Task Handle(IRpcContext<YearChangedEvent> context, CancellationToken token = default)
    {
        YearChanged?.Invoke(context.Message.Year);
        return Task.CompletedTask;
    }
}