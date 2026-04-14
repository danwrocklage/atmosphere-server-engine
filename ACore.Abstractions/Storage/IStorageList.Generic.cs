namespace ACore.Abstractions.Storage;

public interface IStorageList<T>
{
    Task<long> Count();

    Task<bool> Exists(T value);
        
    Task Delete(T value);
        
    Task Store(T value);
        
    Task Store(IList<T> values);

    Task<IEnumerable<T>> GetAll();
}