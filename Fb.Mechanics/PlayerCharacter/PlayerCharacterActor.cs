using ACore.Abstractions.Database;
using AGame.Actors;
using AGame.Transform;
using AUtils.Math;
using Fb.Mechanics.Stats;

namespace Fb.Mechanics.PlayerCharacter;

public class PlayerCharacterActor : Actor
{
    private readonly IDatabase mDatabase;

    public PlayerCharacterActor(IDatabase database)
    {
        mDatabase = database;
    }

    public Guid CharacterId { get; private set; }

    public async Task<bool> LoadCharacter(Guid characterId)
    {
        var entity = await mDatabase.Select<CharacterEntity>()
            .FirstOrDefaultAsync(x => x.Id == characterId);

        if (entity == null)
        {
            Destroy();
            return false;
        }

        CharacterId = entity.Id;
        var transform = Add<CharacterTransformComponent>();
        transform.Position = Point3.FromArray(entity.Position ?? new float[]{0,0,0});
        transform.Direction = Point3.FromArray(entity.Direction ?? new float[]{0,0,0});
        
        var visualState = Add<VisualStateComponent>();
        visualState.Mesh = entity.Mesh;
        visualState.State = entity.State;

        var stats = Add<StatComponent>();
        stats.Load(entity.Stats);

        await mDatabase.Repository<CharacterEntity>()
            .Update(CharacterId)
            .Set(x => x.LastSeenOnline, DateTime.UtcNow)
            .Set(x => x.IsOnline, true)
            .Apply();

        return true;
    }

    protected override async Task OnDestroy()
    {
        var visual = Get<VisualStateComponent>();
        await mDatabase.Repository<CharacterEntity>()
            .Update(x => x.Id == CharacterId)
            .Set(x => x.Position, Get<TransformComponent>().Position.ToArray())
            .Set(x => x.Direction, Get<TransformComponent>().Direction.ToArray())
            .Set(x => x.Mesh, visual.Mesh)
            .Set(x => x.State, visual.State)
            .Set(x => x.Stats, Get<StatComponent>().Store())
            .Set(x => x.LastSeenOnline, DateTime.UtcNow)
            .Set(x => x.IsOnline, false)
            .Apply();
    }
}