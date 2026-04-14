using System.Reflection;
using ACore.Abstractions;
using ACore.Abstractions.Storage;
using ACore.Abstractions.Storage.Geo;
using ACore.Redis.Geo;
using AUtils.IoC;

namespace ACore.Redis;

public class RedisModule : ACore.Modules.Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Singleton<RedisClient, RedisClient, IInitializable>();

        if(!builder.IsRegistered(typeof(IStorageGeo)))
            builder.Singleton<RedisGeo, IStorageGeo>();

        if(builder.IsRegistered(typeof(IStorage)))
            return;

        builder.Singleton<RedisStorage, IStorage>();
        builder.Register(x => x.For(typeof(RedisStorageHash<>), (container, type) =>
        {
            var itemType = type.GetGenericArguments()[0];
            var key = itemType.GetCustomAttribute<StorageKeyAttribute>()?.Key ?? itemType.FullName;
            var getHashMethod = typeof(IStorage)
                .GetMethod(nameof(IStorage.HashOf), BindingFlags.Public | BindingFlags.Instance)?
                .MakeGenericMethod(itemType);
            return getHashMethod?.Invoke(container.Resolve<IStorage>(), new[] {key});
        }).As(typeof(IStorageHash<>)));
        
        builder.Register(x => x.For(typeof(RedisStorageList<>), (container, type) =>
        {
            var itemType = type.GetGenericArguments()[0];
            var key = itemType.GetCustomAttribute<StorageKeyAttribute>()?.Key ?? itemType.FullName;
            var getHashMethod = typeof(IStorage)
                .GetMethod(nameof(IStorage.ListOf), BindingFlags.Public | BindingFlags.Instance)?
                .MakeGenericMethod(itemType);
            return getHashMethod?.Invoke(container.Resolve<IStorage>(), new[] {key});
        }).As(typeof(IStorageList<>)));
    }
}