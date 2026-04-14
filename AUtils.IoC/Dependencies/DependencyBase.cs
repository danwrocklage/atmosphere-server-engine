using System.Linq.Expressions;
using System.Reflection;

namespace AUtils.IoC.Dependencies;

internal abstract class DependencyBase
{
    private bool mIsInitialized;
    private IDependency[] mDependencies;
    protected readonly Type Type;
    private Func<IDependency[], object> mResolver;

    private ConstructorInfo Constructor => Type
        .GetConstructors(BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        .FirstOrDefault();

    protected DependencyBase(Type type)
    {
        Type = type;
    }

    /// <summary>
    /// Prepare dependencies
    /// </summary>
    protected void GetDependencies(IReadOnlyCollection<DependencyBuilder> dependencyBuilders, IReadOnlyCollection<IDependency> staticDependencies)
    {
        var parameters = Constructor?.GetParameters();
        if (parameters == null || mIsInitialized)
            return;

        mDependencies = new IDependency[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i].ParameterType;
            if (parameter.IsGenericType && parameter.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var targetParameter = parameter.GetGenericArguments()[0];
                var elements = dependencyBuilders
                    .Where(x => x.AsTypes.Contains(targetParameter))
                    .Select(x => x.Build())
                    .ToArray();
                    
                mDependencies[i] = new EnumerableDependency(elements, targetParameter);
                continue;
            }

            var openGenericParameter = parameter.IsGenericType ? parameter.GetGenericTypeDefinition() : parameter;
            var injected = dependencyBuilders.FirstOrDefault(x => x.AsTypes.Contains(openGenericParameter));
            if (injected == null)
            {
                mDependencies[i] =
                    staticDependencies.FirstOrDefault(x => x.GetDependencyType() == parameter) ??
                    throw new ResolveException(Type, parameter);
                continue;
            }

            mDependencies[i] = injected.Build();
        }

        mIsInitialized = true;
    }

    /// <summary>
    /// Create object with dependencies
    /// </summary>
    protected object CreateInstance(Type[] genericArgs)
    {
        if (!mIsInitialized)
            throw new InvalidOperationException($"Dependency {Type.Name} is not initialized");

        if (!Type.IsGenericType) 
            return GenerateResolver(Type)(mDependencies);
            
        if (genericArgs is not {Length: > 0})
            throw new InvalidOperationException(
                $"Dependency {Type.Name} is generic, but there are no type arguments");
                
        return GenerateResolver(Type.MakeGenericType(genericArgs))(mDependencies);

    }

    private Func<IDependency[], object> GenerateResolver(Type resolvingType)
    {
        if (Type.IsGenericType && mResolver != null)
            return mResolver;

        var dependencies = Expression.Parameter(typeof(IDependency[]), "dependencies");
        var returnLabel = Expression.Label(resolvingType, "return");

        var argumentTypes = Constructor?.GetParameters()
            .Select(x => x.ParameterType).ToArray() ?? Array.Empty<Type>();
        var arguments = new Expression[argumentTypes.Length];
        for (var j = 0; j < argumentTypes.Length; j++)
        {
            var typeArgs = argumentTypes[j].IsGenericType ? argumentTypes[j].GetGenericArguments() : null;
            if (typeArgs != null && mDependencies.Any(x => x.GetDependencyType() == argumentTypes[j]))
                typeArgs = null;
                
            arguments[j] = Expression.Convert(
                Expression.Call(Expression.ArrayIndex(dependencies, Expression.Constant(j)), nameof(IDependency.Get),
                    Type.EmptyTypes, Expression.Constant(typeArgs, typeof(Type[]))), argumentTypes[j]);
        }

        var constructor = resolvingType
            .GetConstructors(BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .FirstOrDefault();
            
        var body = Expression.Block(
            Expression.Return(returnLabel, constructor != null ? Expression.New(constructor, arguments) : Expression.New(resolvingType)),
            Expression.Label(returnLabel, Expression.Default(resolvingType)));
            
        var resolverExpression = Expression.Lambda<Func<IDependency[], object>>(body, dependencies);
        if(Type.IsGenericType)
            return resolverExpression.Compile();
            
        mResolver = resolverExpression.Compile();
        return mResolver;
    }
}