using ACore.Abstractions.Database;
using AGame.Frontend;
using Fb.Mechanics;
using Fb.Mechanics.PlayerCharacter;

namespace Fb.Frontend.Character;

internal class GetCharactersHandler : PipelineHandler<GetCharactersDto>
{
    private readonly IRepository<CharacterEntity> mCharacterRepository;

    public GetCharactersHandler(IDatabase database)
    {
        mCharacterRepository = database.Repository<CharacterEntity>();
    }

    protected override async Task<object> Handle(GetCharactersDto message, PipelineHandlerContext context)
    {
        var characters = await mCharacterRepository.Select()
            .Where(x => x.AccountId == context.EntityId)
            .Select(x => new CharacterDto
            {
                Id = x.Id,
                Name = x.Name,
                MorphTargets = x.MorphTargets,
                LastSeenOnline = x.LastSeenOnline,
                Mesh = x.Mesh
            })
            .ToListAsync();

        return new GetCharactersResultDto {Characters = characters.ToArray()};
    }
}