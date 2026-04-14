namespace ACore.Abstractions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ConfigurationAttribute : Attribute
{
    public ConfigurationAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}