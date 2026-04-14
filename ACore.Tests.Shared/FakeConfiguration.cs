using ACore.Abstractions;

namespace ACore.Tests.Shared;

internal class FakeConfiguration : IConfigurationManager
{
    public T Get<T>(string key, Func<T> fallback = null) => fallback == null ? default : fallback();

    public T Get<T>(Func<T> fallback = null) => fallback == null ? default : fallback();

    public void AddProvider(IConfigurationProvider provider) { }
}