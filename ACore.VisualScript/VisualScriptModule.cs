using System.Runtime.CompilerServices;
using ACore.Abstractions.Rpc;
using ACore.Modules;
using AUtils.IoC;

[assembly:InternalsVisibleTo("ACore.VisualScript.Tests")]

namespace ACore.VisualScript;

public class VisualScriptModule : ACore.Modules.Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Transient<ScriptService, IScriptService>();
        builder.Transient<ScriptNodeService, IScriptNodeService>();
        builder.Transient<ScriptProcessService, IScriptProcessService>();

        builder.Singleton<ScriptChangedHandler, IRpcHandler<ScriptChangedEvent>>();
    }

    [RoleAny]
    public void Subscribe(CancellationToken token = default)
    {
        Subscribe<ScriptChangedEvent>();
    }
}