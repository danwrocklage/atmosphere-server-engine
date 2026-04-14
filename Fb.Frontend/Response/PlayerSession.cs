using ACore.Abstractions.Storage;
using AGame.Actors.Avatar;
using Fb.Mechanics.PlayerCharacter;

namespace Fb.Frontend.Response;

public class PlayerSession
{
    private readonly IStorage mStorage;
    private readonly AvatarContext mAvatarContext;
    
    private Guid? mCharacterId;

    public PlayerSession(IStorage storage, AvatarContext avatarContext)
    {
        mStorage = storage;
        mAvatarContext = avatarContext;
    }

    public Guid AccountId { get; internal set; }

    public async ValueTask<Guid> CharacterId()
    {
        if (mCharacterId.HasValue)
            return mCharacterId.Value;

        mCharacterId = await mStorage.Get<Guid>($"player:{AccountId}:character");
        return mCharacterId.Value;
    }

    public async Task<AvatarOf<PlayerCharacterActor>> CharacterAvatar()
    {
        return await mAvatarContext.Get<PlayerCharacterActor>(await CharacterId());
    }
}