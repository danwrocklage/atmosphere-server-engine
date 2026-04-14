using System.Reflection;

namespace AUtils.IoC;

public enum RegisterMode : byte
{
    AsSelf,
    AsTarget,
    All
}
    
public static class ContainerBuilderExtensions
{
    public static ContainerBuilder Transient<TImplementation>(this ContainerBuilder builder)
        where TImplementation : class =>
        builder.Register(x => x.For<TImplementation>().AsSelf());

    public static ContainerBuilder Transient<TImplementation, TInterface>(this ContainerBuilder builder)
        where TImplementation : class, TInterface where TInterface : class =>
        builder.Register(x => x.For<TImplementation>().As<TInterface>());

    public static ContainerBuilder Transient<TImplementation, TInterface1, TInterface2>(this ContainerBuilder builder)
        where TImplementation : class, TInterface1, TInterface2 where TInterface1 : class where TInterface2 : class =>
        builder.Register(x => x.For<TImplementation>().As<TInterface1>().As<TInterface2>());

    public static ContainerBuilder Singleton<TImplementation>(this ContainerBuilder builder)
        where TImplementation : class =>
        builder.Register(x => x.For<TImplementation>().AsSelf().Singleton());

    public static ContainerBuilder Singleton<TImplementation, TInterface>(this ContainerBuilder builder)
        where TImplementation : class, TInterface where TInterface : class =>
        builder.Register(x => x.For<TImplementation>().As<TInterface>().Singleton());

    public static ContainerBuilder Singleton<TImplementation, TInterface1, TInterface2>(this ContainerBuilder builder)
        where TImplementation : class, TInterface1, TInterface2 where TInterface1 : class where TInterface2 : class =>
        builder.Register(x => x.For<TImplementation>().As<TInterface1>().As<TInterface2>().Singleton());

    public static ContainerBuilder RegisterBy<T>(this ContainerBuilder builder, RegisterMode mode = RegisterMode.All, bool useCallingAssemblyOnly = false) => 
        RegisterBy(builder, typeof(T), mode, useCallingAssemblyOnly);

    public static ContainerBuilder RegisterBy(this ContainerBuilder builder, Type type, RegisterMode mode = RegisterMode.All, bool useCallingAssemblyOnly = false) =>
        type.IsInterface
            ? RegisterByInterface(builder, type, mode, useCallingAssemblyOnly ? Assembly.GetCallingAssembly().GetTypes() : Types.All)
            : RegisterByBase(builder, type, mode, useCallingAssemblyOnly ? Assembly.GetCallingAssembly().GetTypes() : Types.All);

    private static ContainerBuilder RegisterByBase(ContainerBuilder builder, Type type, RegisterMode mode,  IEnumerable<Type> searchingTypes)
    {
        if (type.IsGenericTypeDefinition)
        {
            var t = searchingTypes
                .Where(x => x.GetParentLike(type) != null && !x.IsAbstract && !x.IsInterface)
                .ToDictionary(x => x.GetParentLike(type), x => x);
            
            foreach (var (genericService, implementation) in t)
                builder.Register(x => Register(x, genericService, implementation, mode));
        }
        else
        {
            var types = searchingTypes
                .Where(x => type.IsAssignableFrom(x) && !x.IsAbstract && !x.IsInterface)
                .ToArray();

            foreach (var t in types)
                builder.Register(x => Register(x, type, t, mode));
        }

        return builder;
    }

    private static ContainerBuilder RegisterByInterface(ContainerBuilder builder, Type type, RegisterMode mode,
        IEnumerable<Type> searchingTypes)
    {
        var condition = type.IsGenericType
            ? t => t.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == type)
            : new Func<Type, bool>(t => t.GetInterfaces().Any(x => x == type));

        var types = searchingTypes
            .Where(x => !x.IsAbstract && !x.IsInterface && condition(x))
            .ToDictionary(x => type.IsGenericType
                ? x.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == type)
                : x, x => x);

        foreach (var (handlerInterface, handler) in types)
            builder.Register(x => Register(x, handlerInterface == handler ? type : handlerInterface, handler, mode));

        return builder;
    }

    private static void Register(DependencyBuilder builder, Type targetType, Type currentType, RegisterMode mode)
    {
        builder.For(currentType);
            
        if (mode is RegisterMode.AsSelf or RegisterMode.All)
            builder.AsSelf();
            
        if (mode is RegisterMode.AsTarget or RegisterMode.All)
            builder.As(targetType);
    }
}