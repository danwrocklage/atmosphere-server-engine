using System.Reflection;

namespace ACore.Abstractions;

/// <summary>
/// Util class for user defined types
/// </summary>
public static class Types
{
    /// <summary>
    /// Get all non system assemblies
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static IEnumerable<Assembly> Assemblies => AppDomain.CurrentDomain.GetAssemblies()
        .Where(x => x.FullName?.StartsWith("System") == false && x.FullName.StartsWith("Microsoft") == false);

    /// <summary>
    /// Get all non system types from all assemblies
    /// </summary>
    public static IEnumerable<Type> All => Assemblies
        .SelectMany(x => x.GetTypes())
        .Where(x => x.FullName?.StartsWith("System") == false && x.FullName?.StartsWith("Microsoft") == false);
    
    /// <summary>
    /// Return parent closed generic of current type
    /// </summary>
    public static Type GetParentLike(this Type type, Type generic)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (generic == null) throw new ArgumentNullException(nameof(generic));
        if (!generic.IsGenericTypeDefinition)
            throw new ArgumentException("Generic type must be open", nameof(generic));
        
        var current = type;
        while (current?.BaseType != null)
        {
            if (current.IsAbstract)
            {
                current = current.BaseType;
                continue;
            }

            if (current.BaseType.IsGenericType &&
                current.BaseType.GetGenericTypeDefinition() == generic)
                return current.BaseType;

            current = current.BaseType;
        }

        return null;
    }
}