namespace ACore.Abstractions.Storage;

public interface IStorageHash<T> : IStorageBase
{
    Task<string[]> GetKeys();
        
    Task Store(string key, T value);
        
    Task Store(IDictionary<string, T> values);

    Task<T> Get(string key);

    Task<IEnumerable<T>> Get(IEnumerable<string> keys);
    Task<IDictionary<string, T>> Get();
        
    Task Increment(string key);
        
    Task Decrement(string key);
}