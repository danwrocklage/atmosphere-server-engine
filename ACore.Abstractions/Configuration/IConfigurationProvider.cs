namespace ACore.Abstractions;

public interface IConfigurationProvider
{
    bool IsExists(string key);
    (T Value, bool IsValueGot) Get<T>(string key);
}