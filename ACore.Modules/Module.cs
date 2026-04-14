using System.Reflection;
using ACore.Abstractions;
using ACore.Abstractions.Rpc;
using ACore.Abstractions.Worker;
using AUtils.IoC;

namespace ACore.Modules;

/// <summary>
/// Main loadable module
/// </summary>
public abstract class Module
{
    /// <summary>
    /// Name of module
    /// </summary>
    public virtual string Name => GetType().Name.Replace("Module", string.Empty);
    
    /// <summary>
    /// DI Container
    /// </summary>
    protected IContainer Services { get; set; }
    
    /// <summary>
    /// Add services to DI container
    /// </summary>
    public abstract void ConfigureServices(ContainerBuilder builder);

    /// <summary>
    /// Run module initialization
    /// </summary>
    public virtual async Task Run(IContainer container, CancellationToken token = default)
    {
        Services = container ?? throw new ArgumentNullException(nameof(container));

        var role = Services.Resolve<ICellEnvironment>().Role ?? throw new CellException();

        var methodsToRun = GetMethods(GetType())
            .Where(x => 
                x.For?.Length == 0 || 
                (x.For?.Contains(role) != false && x.Except?.Contains(role) != true))
            .Select(x => x.Method)
            .ToArray();

        foreach (var method in methodsToRun)
            await method.CreateDelegate<Func<CancellationToken, Task>>(this)(token);
    }

    protected void Subscribe<T>()
    {
        ThrowIfServicesNull();
        Services.Resolve<IRpcSubscribe>().Subscribe<T>();
    }

    protected void Subscribe<T>(params string[] topics)
    {
        ThrowIfServicesNull();
        Services.Resolve<IRpcSubscribe>().Subscribe<T>(topics);
    }

    protected void Worker<T>(CancellationToken token = default) where T : IRunnable
    {
        ThrowIfServicesNull();
        Services.Resolve<ICellWorkers>().Run<T>(
            typeof(T).GetCustomAttribute<WorkerAttribute>()?.Name ?? typeof(T).FullName,
            token);
    }
    
    protected void Worker<T>(string cron, CancellationToken token = default) where T : IRunnable
    {
        if (string.IsNullOrEmpty(cron))
            throw new ArgumentNullException(nameof(cron));
        
        ThrowIfServicesNull();
        Services.Resolve<ICellWorkers>().Run<T>(
                typeof(T).GetCustomAttribute<WorkerAttribute>()?.Name.Replace('-', '.') ?? 
                typeof(T).FullName,
            token, cron);
    }

    private static (string[] For, string[] Except, MethodInfo Method)[] GetMethods(Type type)
    {
        if (type == null) 
            throw new ArgumentNullException(nameof(type));

        var result = new List<(string[] For, string[] Except, MethodInfo Method)>();
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != 1 ||
                parameters[0].ParameterType != typeof(CancellationToken) ||
                method.ReturnType != typeof(Task))
                continue;
            
            var forRoles = method.GetCustomAttribute<RoleAnyAttribute>()?.Cells;
            var exceptRoles = method.GetCustomAttribute<RoleExceptAttribute>()?.Cells;
            
            if(forRoles == null && exceptRoles == null)
                continue;
            
            if (forRoles != null && exceptRoles != null)
            {
                forRoles = forRoles.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
                exceptRoles = exceptRoles.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
                
                if (forRoles.Length == 0 && exceptRoles.Length == 0)
                    throw new CellException(
                        $"Module run method {method.DeclaringType?.Name}.{method.Name} can't include and exclude all roles at same time");
                
                var rolesIntersections = forRoles.Intersect(exceptRoles).ToArray();
                if (rolesIntersections.Length > 0)
                    throw new CellException(
                        $"Module run method {method.DeclaringType?.Name}.{method.Name} can't include and exclude role at same time: {string.Join(',', rolesIntersections)}");
            }
            
            result.Add((forRoles, exceptRoles, method));
        }

        return result.ToArray();
    }

    private void ThrowIfServicesNull()
    {
        if (Services == null)
            throw new CellException( $"{nameof(Services)} is null");
    }
}