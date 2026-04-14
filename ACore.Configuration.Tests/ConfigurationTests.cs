using System;
using System.Collections.Generic;
using ACore.Abstractions;
using Xunit;

namespace ACore.Configuration.Tests;

public class ConfigurationTests
{
    [Fact]
    public void GetConfigurationSectionTest()
    {
        var values = new Dictionary<string, object>
        {
            {"TestAttrSection", new TestSection {A = "SomeString", B = Guid.NewGuid()}},
            {"test_string1", "SomeSSS"},
            {"test_number", 124346}
        };
        var config = new Configuration(new[] {new TestConfigurationProvider(values)});
        
        Assert.Equal("SomeSSS", config.Get("test_string1", () => "SomeNotSS"));
        Assert.Equal(124346, config.Get("test_number", () => 11));

        var section = config.Get(() => new TestSection());
        Assert.Equal(((TestSection) values["TestAttrSection"]).A, section.A);
        Assert.Equal(((TestSection) values["TestAttrSection"]).B, section.B);
    
        Assert.Equal("SomeNotSS", config.Get("not_existed_section", () => "SomeNotSS"));
    }

    [Fact]
    public void AddConfigurationProviderTest()
    {
        var config = new Configuration(Array.Empty<IConfigurationProvider>());
        Assert.Equal("SomeNotSS", config.Get("test_string1", () => "SomeNotSS"));
        
        var values = new Dictionary<string, object> {{"test_string1", "SomeSSS"}};
        config.AddProvider(new TestConfigurationProvider(values));
        
        Assert.Equal("SomeSSS", config.Get("test_string1", () => "SomeNotSS"));
    }
}