using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using ACore.Abstractions.Database;

namespace ACore.Tests.Shared.Database;

public class FakeDatabase : IDatabase
{
    private readonly Dictionary<string, object> mRepositories = new();

    public FakeRepository<T> GetFakeRepository<T>() where T : IDbEntity
    {
        var type = typeof(T);
        var name = type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name;

        return GetFakeRepository<T>(name);
    }

    public FakeRepository<T> GetFakeRepository<T>(string name) where T : IDbEntity
    {
        if (mRepositories.TryGetValue(name, out var repository))
            return (FakeRepository<T>) repository;

        repository = new FakeRepository<T>();
        mRepositories.Add(name, repository);        
        return (FakeRepository<T>) repository;
    }

    public IRepository<T> Repository<T>() where T : IDbEntity => GetFakeRepository<T>();

    public IRepository<T> Repository<T>(string name) where T : IDbEntity => GetFakeRepository<T>(name);
}