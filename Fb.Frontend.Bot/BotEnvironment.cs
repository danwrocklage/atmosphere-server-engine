using ACore.Abstractions;

namespace Fb.Frontend.Bot;

internal class BotEnvironment : ICellEnvironment
{
    public string Role => "Atmosphere Engine Game Bot";
    public string Configuration => "Development";
    public string Build => "Debug";
    public string Endpoint => string.Empty;
    public bool IsContainerBuild => false;
    
    public override string ToString() => 
        ((ICellEnvironment)this).ToString(false);
}