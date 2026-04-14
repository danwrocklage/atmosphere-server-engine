using System.Reflection;
using ACore.Worker.Web.Routing;

namespace ACore.Worker.Web;

public static class PipelineBuilderExtensions
{
    /// <summary>
    /// Add all controllers types from assembly
    /// </summary>
    public static PipelineBuilder UseAssemblyControllers(this PipelineBuilder builder, Assembly assembly = null)
    {
        var controllers = (assembly ?? Assembly.GetCallingAssembly())
            .GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract && x.IsAssignableTo(typeof(Controller)))
            .ToArray();

        foreach (var controller in controllers)
            builder.UseController(controller);

        return builder;
    }
}