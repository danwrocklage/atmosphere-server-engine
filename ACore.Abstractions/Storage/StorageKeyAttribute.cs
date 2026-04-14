namespace ACore.Abstractions.Storage;

/// <summary>
/// Specify storage hash or list key 
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class StorageKeyAttribute : Attribute
{
    public StorageKeyAttribute(string key)
    {
        Key = key;
    }

    /// <summary>
    /// Hash or list storage key
    /// </summary>
    public string Key { get; }
}