using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ACore.Worker.Web.Routing.Info;

/// <summary>
/// Route info view model
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
internal class RouteInfoViewModel
{
    internal RouteInfoViewModel(RouteInfo info)
    {
        Name = info.Action.Name;
        Description = info.Action.GetCustomAttribute<DescriptionAttribute>()?.Description;
        Method = info.Method;
        Path = info.Path;
        TypeInfo = info.Controller?.FullName?.Replace("controller", "", StringComparison.InvariantCultureIgnoreCase);
        Parameters = info.Parameters
            .Select(x => new Parameter(x))
            .ToArray();
    }
    
    /// <summary>
    /// Controller action name
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Url template path
    /// </summary>
    public string Path { get; }
        
    /// <summary>
    /// Http method
    /// </summary>
    public string Method { get; }
    
    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Controller type full name
    /// </summary>
    public string TypeInfo { get; }

    /// <summary>
    /// Arguments (without Service and Stream types)
    /// </summary>
    public Parameter[] Parameters { get; }
    
    internal class Parameter
    {
        internal Parameter(RouteInfo.Parameter parameter)
        {
            Name = parameter.Name;
            ParameterType = parameter.ParameterType;
            TypeInfo = parameter.Type?.FullName;
            IsRequired = parameter.IsRequired;
        }
    
        public string Name { get; }
        
        public string TypeInfo { get; }
    
        public string ParameterType { get; }

        public bool IsRequired { get; }
    }
}