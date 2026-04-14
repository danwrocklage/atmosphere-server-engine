using AGame.Frontend;
using AGame.Time;

namespace Fb.Frontend.System.Time;

public class GetNowHandler : PipelineHandler<GetNowDto>
{
    private readonly IGameTimeService mGameTimeService;

    public GetNowHandler(IGameTimeService gameTimeService)
    {
        mGameTimeService = gameTimeService;
    }

    protected override async Task<object> Handle(GetNowDto message, PipelineHandlerContext context)
    {
        var now = DateTime.UtcNow;
        return new GetNowResultDto
        {
            GameNow = await mGameTimeService.Now(),
            UtcNow = now
        };
    }
}