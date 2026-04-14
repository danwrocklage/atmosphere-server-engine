using System.IO;
using ACore.Configuration.Providers;
using Xunit;

namespace ACore.Configuration.Tests;

internal class JsonSection
{
    public string Field1 { get; set; }
    public int Field2 { get; set; }
}

public class ProvidersTests
{
    [Fact]
    public void JsonConfigurationProviderTest()
    {
        var json = File.ReadAllText("test_config.json");
        var provider = new JsonConfigurationProvider(json);
        
        Assert.True(provider.IsExists("test_section"));
        Assert.False(provider.IsExists("unexists_section"));

        var section = provider.Get<string[]>("test_array");
        Assert.True(section.IsValueGot);
        Assert.Equal(new [] {"array item 1"}, section.Value);

        var s = provider.Get<JsonSection>("test_section");
        Assert.True(s.IsValueGot);
        Assert.Equal("some test string", s.Value.Field1);
        Assert.Equal(222, s.Value.Field2);
    }
    
    [Fact(Skip = "Environment mock required")]
    public void ArgsConfigurationProviderTest()
    {
        var provider = new ArgsConfigurationProvider();
    }
    
    [Fact(Skip = "Environment mock required")]
    public void EnvVarConfigurationProviderTest()
    {
        var provider = new EnvVarConfigurationProvider();
    }
}