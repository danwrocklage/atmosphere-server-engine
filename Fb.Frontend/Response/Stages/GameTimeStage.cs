using AGame.Time;
using Fb.Frontend.System.Time;

namespace Fb.Frontend.Response.Stages;

public class GameTimeStage : IResponseStage
{
    private readonly IGameTimeService mGameTimeService;

    public GameTimeStage(IGameTimeService gameTimeService)
    {
        mGameTimeService = gameTimeService;
    }

    public async Task<object> Execute(PlayerSession session, CancellationToken token = default)
    {
        var now = DateTime.UtcNow;
        return new GetNowResultDto
        {
            GameNow = await mGameTimeService.Now(),
            UtcNow = now
        };
    }
}