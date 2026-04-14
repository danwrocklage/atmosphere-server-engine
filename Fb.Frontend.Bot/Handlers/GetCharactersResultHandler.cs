using ACore.Abstractions;
using ACore.Abstractions.Logging;
using AGame.Frontend;
using Fb.Frontend.Character;

namespace Fb.Frontend.Bot.Handlers;

[Log(Category = "game")]
public class GetCharactersResultHandler : PipelineHandler<GetCharactersResultDto>
{
    private readonly ILogger<GetCharactersResultHandler> mLogger;
    private readonly IConfiguration mConfiguration;

    public GetCharactersResultHandler(ILogger<GetCharactersResultHandler> logger, IConfiguration configuration)
    {
        mLogger = logger;
        mConfiguration = configuration;
    }

    protected override Task<object> Handle(GetCharactersResultDto message, PipelineHandlerContext context)
    {
        if (message.Characters == null || message.Characters.Length == 0)
        {
            mLogger.Info("There is no any character. Create it.");
            return Task.FromResult<object>(mConfiguration.Get("character", () => new CreateCharacterDto
            {
                Name = "DefaultBot",
                MorphTargets = new Dictionary<string, float>()
            }));
        }
        
        mLogger.Info($"Select character {message.Characters[0].Name} ({message.Characters[0].Id})");
        return Task.FromResult<object>(new WorldEnterDto {CharacterId = message.Characters[0].Id});
    }
}