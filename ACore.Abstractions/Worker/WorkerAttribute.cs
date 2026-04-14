namespace ACore.Abstractions.Worker;

/// <summary>
/// Name for worker type
/// </summary>
public class WorkerAttribute : Attribute
{
    public WorkerAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}