using AGame.Frontend;
using Fb.Mechanics.PlayerCharacter;

namespace Fb.Frontend.Character;

public class CreateCharacterHandler : PipelineHandler<CreateCharacterDto>
{
    private readonly ICharacterCreationService mCharacterCreationService;

    public CreateCharacterHandler(ICharacterCreationService characterCreationService)
    {
        mCharacterCreationService = characterCreationService;
    }

    protected override async Task<object> Handle(CreateCharacterDto body, PipelineHandlerContext context)
    {
        if (string.IsNullOrEmpty(body?.Name) || body.MorphTargets == null ||
            !mCharacterCreationService.ValidateName(body.Name) ||
            body.MorphTargets.Keys.Any(x => string.IsNullOrEmpty(x)) ||
            !await mCharacterCreationService.CanAccountCreateCharacter(context.EntityId) ||
            await mCharacterCreationService.IsNameAlreadyUsed(body.Name))
        {
            return CreateCharacterResultDto.Fail;
        }

        await mCharacterCreationService.CreateCharacter(context.EntityId, body.Name, body.MorphTargets);
        return CreateCharacterResultDto.Success;
    }
}