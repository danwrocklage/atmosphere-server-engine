using System;
using ACore.Abstractions;

namespace ACore.Configuration.Tests;

[Configuration("TestAttrSection")]
internal class TestSection
{
    public string A { get; set; }
    
    public Guid B { get; set; }
}