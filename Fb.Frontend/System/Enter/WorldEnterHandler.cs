using ACore.Abstractions.Database;
using ACore.Abstractions.Storage;
using AGame.Actors.Avatar;
using AGame.Frontend;
using AGame.Transform;
using Fb.Mechanics;
using Fb.Mechanics.PlayerCharacter;

namespace Fb.Frontend;

/// <summary>
/// Player enter to the game world
/// </summary>
public class WorldEnterHandler : PipelineHandler<WorldEnterDto>
{
    private readonly AvatarContext mAvatarContext;
    private readonly IStorage mStorage;
    private readonly IAfkTracker mAfkTracker;
    private readonly IRepository<CharacterEntity> mCharacterRepository;

    public WorldEnterHandler(AvatarContext avatarContext, IDatabase database, IStorage storage, IAfkTracker afkTracker)
    {
        mAvatarContext = avatarContext;
        mStorage = storage;
        mAfkTracker = afkTracker;
        mCharacterRepository = database.Repository<CharacterEntity>();
    }

    protected override async Task<object> Handle(WorldEnterDto message, PipelineHandlerContext context)
    {
        if (message.CharacterId == default)
            context.Close();

        var avatar = await mAvatarContext.Create<PlayerCharacterActor>($"player:{context.EntityId.ToString()}", token: context.CancellationToken);
        var loaded = await avatar
            .Rpc(x => x.LoadCharacter(message.CharacterId), context.CancellationToken);
        if (!loaded)
            return context.Close();

        await mStorage.Store($"player:{context.EntityId}:character", avatar.Id);
        context.OnClose += () => mStorage.Delete($"player:{context.EntityId}:character"); 

        var tca = await avatar.Get<TransformComponent>();
        var position = await tca.Rpc(x => x.Position, context.CancellationToken);
        var direction = await tca.Rpc(x => x.Direction, context.CancellationToken);

        var morphs = await mCharacterRepository.Select()
            .Where(x => x.Id == message.CharacterId)
            .Select(x => x.MorphTargets)
            .FirstOrDefaultAsync(context.CancellationToken);

        mAfkTracker.UpdateTime(avatar.Id);
        context.OnClose += () =>
        {
            mAfkTracker.Remove(avatar.Id);
            return Task.CompletedTask;
        };
        
        return new WorldEnterResultDto
        {
            Direction = direction,
            Position = position,
            MorphTargets = morphs
        };
    }
}