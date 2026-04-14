using System.Runtime.CompilerServices;
using ACore.Worker.Web.Routing;
using ACore.Worker.Web.Routing.Info;
using AUtils.IoC;

namespace ACore.Worker.Web;

/// <summary>
/// Configure http request processing pipeline
/// </summary>
public class PipelineBuilder
{
    private readonly List<Type> mControllers;
    private readonly List<Type> mMiddlewares;
    private readonly Pipeline mPipeline;
    private readonly IContainer mContainer;
        
    internal PipelineBuilder(Pipeline pipeline, IContainer container)
    {
        mPipeline = pipeline;
        mContainer = container;
        mMiddlewares = new List<Type>();
        mControllers = new List<Type>();
    }

    /// <summary>
    /// Add endpoints browser UI
    /// </summary>
    public PipelineBuilder UseRoutingInfo()
    {
        UseController(typeof(RouteInfoController));
        return this;
    }
        
    /// <summary>
    /// Use processing module
    /// </summary>
    public PipelineBuilder UseModule<T>() where T: Module
    {
        var module = (Module) mContainer.Resolve<T>();
        module.Configure(this);
        return this;
    }
        
    /// <summary>
    /// Use processing class for all http requests
    /// </summary>
    public PipelineBuilder UseMiddleware<T>() where T: Middleware
    {
        mMiddlewares.Add(typeof(T));
        return this;
    }
        
    /// <summary>
    /// Use endpoint processing class
    /// </summary>
    public PipelineBuilder UseController<T>() where T: Controller
    {
        UseController(typeof(T));
        return this;
    }

    /// <summary>
    /// Use endpoint processing class
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UseController(Type type) => mControllers.Add(type);

    /// <summary>
    /// Prepare <see cref="Pipeline"/> instance
    /// </summary>
    internal Pipeline Build()
    {
        var middlewares = mMiddlewares
            .Select(x => (Middleware) mContainer.Resolve(x))
            .ToArray();
            
        mPipeline.Initialize(middlewares, mControllers);
        return mPipeline;
    }
}