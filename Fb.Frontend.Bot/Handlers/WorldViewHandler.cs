using System.Text;
using ACore.Abstractions.Logging;
using AGame.Frontend;

namespace Fb.Frontend.Bot.Handlers;

public class WorldViewHandler : PipelineHandler<WorldViewDto>
{
    private readonly ILogger<WorldViewHandler> mLogger;

    public WorldViewHandler(ILogger<WorldViewHandler> logger)
    {
        mLogger = logger;
    }

    protected override async Task<object> Handle(WorldViewDto message, PipelineHandlerContext context)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"World view {message.Items.Length}");
        foreach (var item in message.Items)
        {
            builder.AppendLine($"{item.Id.ToString()} {(item.Cached ? "(cached)" : string.Empty)}");
            builder.AppendLine($"{item.Position.ToString()} Mesh: {item.Mesh}, State: {item.State}");
        }
        mLogger.Info(builder.ToString());
        
        await Task.Delay(500);
        return ActionDto.Idle;
    }
}