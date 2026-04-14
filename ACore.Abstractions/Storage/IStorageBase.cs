namespace ACore.Abstractions.Storage;

public interface IStorageBase
{
    Task<bool> Exists(string key);
        
    Task Delete(string key);
}