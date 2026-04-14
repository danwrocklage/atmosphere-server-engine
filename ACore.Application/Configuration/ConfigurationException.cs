namespace ACore.Application.Configuration;

/// <summary>
/// Internal configuration exception
/// </summary>
internal class ConfigurationException : ApplicationException
{
    public ConfigurationException(string message) : base(message)
    {
    }
}