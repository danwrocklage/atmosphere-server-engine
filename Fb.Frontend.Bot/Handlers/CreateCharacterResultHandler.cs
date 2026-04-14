using ACore.Abstractions.Logging;
using AGame.Frontend;
using Fb.Frontend.Character;

namespace Fb.Frontend.Bot.Handlers;

[Log(Category = "game")]
public class CreateCharacterResultHandler : PipelineHandler<CreateCharacterResultDto>
{
    private readonly ILogger<CreateCharacterResultHandler> mLogger;

    public CreateCharacterResultHandler(ILogger<CreateCharacterResultHandler> logger)
    {
        mLogger = logger;
    }

    protected override Task<object> Handle(CreateCharacterResultDto message, PipelineHandlerContext context)
    {
        if (message.IsSuccess)
        {
            mLogger.Success("The character was successfully created");
            return Task.FromResult(GetCharactersDto.Instance);
        }

        mLogger.Warn("Failed to create character");
        return Task.FromResult(context.Close());
    }
}