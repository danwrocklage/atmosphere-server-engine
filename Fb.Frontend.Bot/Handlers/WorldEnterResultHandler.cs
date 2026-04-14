using ACore.Abstractions.Logging;
using AGame.Frontend;
using Fb.Frontend.Developer;

namespace Fb.Frontend.Bot.Handlers;

[Log(Category = "game")]
public class WorldEnterResultHandler : PipelineHandler<WorldEnterResultDto>
{
    private readonly ILogger<WorldEnterResultHandler> mLogger;

    public WorldEnterResultHandler(ILogger<WorldEnterResultHandler> logger)
    {
        mLogger = logger;
    }

    protected override async Task<object> Handle(WorldEnterResultDto message, PipelineHandlerContext context)
    {
        mLogger.Success($"Entered to game. Position: {message.Position}, Direction: {message.Direction}. Morphs: {message.MorphTargets.Length}");

        await Task.Delay(500);

        return CreateBoxesRequest.Instance;
    }
}