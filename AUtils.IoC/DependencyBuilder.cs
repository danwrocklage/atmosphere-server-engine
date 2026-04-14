using System.Diagnostics;
using AUtils.IoC.Dependencies;

namespace AUtils.IoC;

[DebuggerDisplay("Builder for {mForType.Name} {mAsSingleton ? \"as singleton\" : \"\"}")]
public class DependencyBuilder
{
    private readonly ContainerBuilder mBuilder;
    private bool mAsSingleton;
    private Type mForType;
    private readonly List<IDependency> mStaticDependencies = new();
    private IDependency mBuiltDependency;
    private Delegate mCustomResolver;

    internal HashSet<Type> AsTypes { get; } = new();

    internal DependencyBuilder(ContainerBuilder builder)
    {
        mBuilder = builder;
    }

    public DependencyBuilder Singleton()
    {
        mAsSingleton = true;
        return this;
    }

    public DependencyBuilder For(Type type)
    {
        TryAddForType(type);
        return this;
    }

    public DependencyBuilder For<T>() where T : class
    {
        TryAddForType(typeof(T));
        return this;
    }

    public DependencyBuilder For<T>(Func<T> producer) where T : class
    {
        if (producer == null) 
            throw new ArgumentNullException(nameof(producer));
            
        mBuiltDependency = new SingletonDependency(producer());
        mAsSingleton = true;
        mForType = typeof(T);
        return this;
    }

    public DependencyBuilder For<T>(Func<IContainer, Type, T> resolver) where T : class
    {
        mCustomResolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        mForType = typeof(T);
        return this;
    }
    
    public DependencyBuilder For(Type forType, Func<IContainer, Type, object> resolver)
    {
        mCustomResolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        TryAddForType(forType);
        return this;
    }

    public DependencyBuilder AsSelf()
    {
        As(mForType);
        return this;
    }

    public DependencyBuilder As(Type type)
    {
        TryAddAsType(type);
        return this;
    }

    public DependencyBuilder As<T>() where T : class
    {
        TryAddAsType(typeof(T));
        return this;
    }

    public DependencyBuilder Add<T>(Func<T> staticDependency) where T : class
    {
        mStaticDependencies.Add(new StaticDependency(typeof(T), staticDependency));
        return this;
    }

    internal IDependency Build()
    {
        if (mBuiltDependency != null)
            return mBuiltDependency;

        if (mCustomResolver != null)
            mBuiltDependency = new ContainerDependency(mForType, (Func<IContainer, Type, object>) mCustomResolver,
                mBuilder.DependencyItems);
        else
        {
            mBuiltDependency = mAsSingleton
                ? new SingletonDependency(mForType, mBuilder.DependencyItems, mStaticDependencies)
                : new DynamicDependency(mForType, mBuilder.DependencyItems, mStaticDependencies);
        }

        return mBuiltDependency;
    }

    private void TryAddAsType(Type type)
    {
        if (mForType == null)
            throw new ArgumentException($"{nameof(mForType)} is not assigned");

        if (type == mForType && !AsTypes.Contains(type))
        {
            AsTypes.Add(type);
            return;
        }

        if (type.IsInterface)
        {
            if(mForType.IsGenericType && type.IsGenericType)
            {
                if (!mForType.GetInterfaces()
                        .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == type.GetGenericTypeDefinition()))
                    throw new ArgumentException($"Interface {type.FullName} is not implemented by {mForType.FullName}");
            }
            else if (!mForType.GetInterfaces().Contains(type))
                throw new ArgumentException($"Interface {type.FullName} is not implemented by {mForType.FullName}");

            AsTypes.Add(type);
            return;
        }

        var current = mForType;
        var found = false;
        while (current.BaseType != null)
        {
            if (current.BaseType == type)
            {
                found = true;
                break;
            }

            current = current.BaseType;
        }

        if (!found)
            throw new ArgumentException($"Type [{type.FullName}] is not assignable for ForType");

        AsTypes.Add(type);
    }

    private void TryAddForType(Type type)
    {
        if(type.IsInterface || type.IsAbstract || type.IsEnum)
            throw new ArgumentException($"Wrong type for ForType {type.Name}");

        if (mForType != null)
            throw new ArgumentException("For type already assigned");

        mForType = type;
    }
}