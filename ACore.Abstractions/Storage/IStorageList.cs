namespace ACore.Abstractions.Storage;

public interface IStorageList
{
    Task<long> Count();
        
    Task<bool> Exists<T>(T value);
        
    Task Delete<T>(T value);
        
    Task Store<T>(T value);
        
    Task Store<T>(IList<T> values);

    Task<IEnumerable<T>> GetAll<T>();
}