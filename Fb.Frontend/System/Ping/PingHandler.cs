using AGame.Frontend;

namespace Fb.Frontend.System.Ping;

public class PingHandler : PipelineHandler<PingDto>
{
    protected override Task<object> Handle(PingDto message, PipelineHandlerContext context) => 
        Task.FromResult(PingDto.Instance);
}