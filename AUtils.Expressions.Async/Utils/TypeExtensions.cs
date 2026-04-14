using System.Diagnostics.CodeAnalysis;

namespace AUtils.Expressions.Async;

internal static class TypeExtensions
{
    public static Type[] GetGenericArguments([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type, Type genericDefinition)
        => FindGenericInstance(type, genericDefinition)?.GetGenericArguments() ?? Type.EmptyTypes;
    
    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1013", Justification = "False positive")]
    internal static Type? FindGenericInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type, Type genericDefinition)
    {
        bool fIsGenericInstanceOf(Type candidate)
            => candidate.IsConstructedGenericType && candidate.GetGenericTypeDefinition() == genericDefinition;

        if (type.IsGenericTypeDefinition || !genericDefinition.IsGenericTypeDefinition)
            return null;

        if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == genericDefinition)
            return type;

        switch (genericDefinition)
        {
            case { IsSealed: true }:
                return fIsGenericInstanceOf(type) ? type : null;
            case { IsInterface: true }:
                foreach (var iface in type.GetInterfaces())
                {
                    if (fIsGenericInstanceOf(iface))
                        return iface;
                }

                break;
            default:
                for (Type? lookup = type; lookup is not null; lookup = lookup.BaseType)
                {
                    if (fIsGenericInstanceOf(lookup))
                        return lookup;
                }

                break;
        }

        return null;
    }
}