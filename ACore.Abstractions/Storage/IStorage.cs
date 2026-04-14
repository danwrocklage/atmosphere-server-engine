namespace ACore.Abstractions.Storage;

public interface IStorage : IStorageBase
{
    Task Transaction(Func<IStorage, Task> transaction);
        
    Task Store<T>(string key, T value, TimeSpan expire);
        
    Task Store<T>(string key, T value);
        
    Task<T> Get<T>(string key);

    IStorageHash<THash> HashOf<THash>(string key);
        
    IStorageList<TItem> ListOf<TItem>(string key);

    Task Increment(string key);
        
    Task Decrement(string key);
}