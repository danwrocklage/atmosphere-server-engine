using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using ACore.Abstractions;
using ACore.Abstractions.Worker;

namespace ACore.Application.Workers;

internal partial class CellWorkersService
{
    private static readonly ReadOnlyDictionary<string, Type> sWorkerTypes;

    static CellWorkersService()
    {
        var workerTypes = Types.All
            .Where(x => x.GetInterfaces().Any(i => i == typeof(IRunnable)))
            .ToArray();

        var workers = new Dictionary<string, Type>();
        foreach (var workerType in workerTypes)
        {
            var name = workerType.GetCustomAttribute<WorkerAttribute>();
            if(name == null)
                continue;

            if (workers.ContainsKey(name.Name))
                throw new ArgumentException($"Worker with '{name.Name}' already exists");
            
            if(workerType.GetInterfaces().All(x => x != typeof(IRunnable)))
                throw new ArgumentException($"Worker with '{name.Name}' already exists");

            workers.Add(name.Name, workerType);
        }

        sWorkerTypes = new ReadOnlyDictionary<string, Type>(workers);
    }

    public static Type GetWorkerType(string name) => sWorkerTypes.TryGetValue(name, out var t) ? t : null;

    public static string GetWorkerName(Type type) => sWorkerTypes.SingleOrDefault(x => x.Value == type).Key;

    public static string[] WorkerNames => sWorkerTypes.Keys.ToArray();
}