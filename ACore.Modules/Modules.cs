using System.Reflection;
using ACore.Abstractions;
using ACore.Application;

namespace ACore.Modules;

public static class Modules
{
    /// <summary>
    /// Search for all module's types and create their instances
    /// </summary>
    public static Module[] Create()
    {
        DebugLogger.WriteLine("Start creating modules...");

        var discoveredModulesTypes = Types.All
            .Where(x => x.BaseType == typeof(Module) && x.IsClass && !x.IsAbstract)
            .OrderBy(x => x.GetCustomAttribute<OrderAttribute>()?.Order ?? default)
            .ThenBy(x => x.Name)
            .ToList();

        var modules = new List<Module>(discoveredModulesTypes.Count);
        foreach (var moduleType in discoveredModulesTypes)
        {
            DebugLogger.WriteLine($"Create module: {moduleType.FullName}");

            try
            {
                var module = (Module) Activator.CreateInstance(moduleType);
                if (module == null)
                {
                    DebugLogger.WriteLine($"Can't create module {moduleType.FullName}", ConsoleColor.Yellow);
                    continue;
                }

                modules.Add(module);
            }
            catch (TargetInvocationException e)
            {
                DebugLogger.WriteLine($"Can't create module {moduleType.FullName}. {e.InnerException?.Message}",
                    ConsoleColor.Yellow);
            }
            catch (Exception e)
            {
                DebugLogger.WriteLine($"Can't create module {moduleType.FullName}. {e.Message}", ConsoleColor.Yellow);
            }
        }

        return modules.ToArray();
    }
}