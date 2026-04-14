using System.Reflection;

namespace AUtils.Sil.Compiler;

internal static class TypeLoader
{
    public static PropertyInfo[] GetSilProps(this Type silType) =>
        silType.GetProperties()
            .Where(x => 
                x.CanRead && x.CanWrite && 
                x.GetCustomAttribute<SilIgnoreAttribute>() == null)
            .ToArray();

    private static Type[] GetSilDependencies(this Type silType)
    {
        var props = silType.GetSilProps();

        var types = new List<Type>();
        types.AddRange(props
            .Where(x => x.PropertyType is {IsEnum: false, IsArray: false} && x.PropertyType.FullName?.StartsWith("System.") == false)
            .Select(x => x.PropertyType)
            .ToArray());
        
        types.AddRange(props
            .Where(x => x.PropertyType.FullName?.StartsWith("System.") == false && x.PropertyType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            .Select(x => x.PropertyType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(i => i.GetGenericArguments().First())
                .FirstOrDefault())
            .ToArray());

        return types.ToArray();
    }

    public static List<(ushort, Type)> LoadTypes(Assembly assembly)
    {
        var result = new List<Type>();
        foreach (var x in assembly.GetTypes())
        {
            if(x.IsInterface || x.IsAbstract || x.GetCustomAttribute<SilAttribute>() == null)
                continue;

            result.Add(x);
            LoadSubTypes(x, result);
        }

        var resultTypes = result.Distinct().Select(
            x => (x.GetCustomAttribute<SilAttribute>()?.Index ?? default, Type: x)).ToList();

        resultTypes.ForEach(x => Console.WriteLine($"{x.Item1} => {x.Type.FullName}"));

        var notFoundIndexes = resultTypes.Where(x => x.Item1 < 1).ToArray();
        if (notFoundIndexes.Length > 0)
            throw new ApplicationException($"There are {notFoundIndexes.Length} types with invalid Sil index")
                {Data = {{"Items", notFoundIndexes}}};
        
        return resultTypes;
    }

    private static void LoadSubTypes(Type x, List<Type> result)
    {
        var props = x.GetSilDependencies();
        result.AddRange(props);
        foreach (var prop in props)
            LoadSubTypes(prop, result);
    }
}