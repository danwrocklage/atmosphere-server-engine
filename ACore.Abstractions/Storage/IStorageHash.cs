namespace ACore.Abstractions.Storage;

public interface IStorageHash : IStorageBase
{
    Task<string[]> GetKeys();
        
    Task Store<T>(string key, T value);
        
    Task Store<T>(IDictionary<string, T> values);

    Task<T> Get<T>(string key);

    Task<IEnumerable<T>> Get<T>(IEnumerable<string> keys);
        
    Task<IDictionary<string, T>> Get<T>();

    Task Increment(string key);
        
    Task Decrement(string key);
}