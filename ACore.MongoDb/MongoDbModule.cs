using System.Reflection;
using ACore.Abstractions;
using ACore.Abstractions.Database;
using AUtils.IoC;
using MongoDB.Bson.Serialization;

namespace ACore.MongoDb;

public class MongoDbModule : ACore.Modules.Module
{
    static MongoDbModule()
    {
        BsonSerializer.RegisterSerializationProvider(new BsonStructSerializerProvider());
    }
    
    public override void ConfigureServices(ContainerBuilder builder)
    {
        if (builder.IsRegistered<IDatabase>())
            throw new InvalidOperationException($"{nameof(IDatabase)} is already registered in container");
        
        builder.Singleton<MongoDatabase, IDatabase, IAsyncInitializable>();
        builder.Register(x => x.For(typeof(MongoRepository<>), (container, type) =>
        {
            var itemType = type.GetGenericArguments()[0];
            var getRepoMethod = typeof(IDatabase)
                .GetMethod(nameof(IDatabase.Repository), BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes)?
                .MakeGenericMethod(itemType);
            return getRepoMethod?.Invoke(container.Resolve<IDatabase>(), Array.Empty<object>());
        }).As(typeof(IRepository<>)));
    }
}