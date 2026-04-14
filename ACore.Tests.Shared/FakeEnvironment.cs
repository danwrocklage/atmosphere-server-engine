using ACore.Abstractions;

namespace ACore.Tests.Shared;

internal class FakeEnvironment : ICellEnvironment
{
    public string Role => "TestRole";
    
    public string Configuration => "TestConfiguration";
    
    public string Build => "TestBuild";
    
    public string Endpoint => "TestEndpoint";
    
    public bool IsContainerBuild => false;
    
    public override string ToString() => 
        ((ICellEnvironment)this).ToString(false);
}