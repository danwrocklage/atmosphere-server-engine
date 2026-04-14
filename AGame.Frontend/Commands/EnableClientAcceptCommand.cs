using System.Collections.Specialized;
using System.ComponentModel;
using ACore.Abstractions;
using ACore.Abstractions.Logging;

namespace AGame.Frontend.Commands;

[DisplayName("frontend.enable")]
internal class EnableClientAcceptCommand : ICommandHandler
{
    private readonly ConnectionEnableService mConnectionEnableService;

    public EnableClientAcceptCommand(ConnectionEnableService connectionEnableService)
    {
        mConnectionEnableService = connectionEnableService;
    }

    public Task<object> Run(NameValueCollection queryParams, CancellationToken token)
    {
        if (!bool.TryParse(queryParams["enabled"], out var value))
            return null;
        
        mConnectionEnableService.IsEnable = value;
        return null;
    }
}