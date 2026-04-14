namespace ACore.Abstractions.Storage.Files;

public interface IStorageFiles
{
    Task Upload(string path, string fileName, Stream content);
    
    Task<bool> Exists(string path);
    
    Task<bool> Exists(string path, string fileName);
    
    Task Delete(string path);
    
    Task Delete(string path, string fileName);
}