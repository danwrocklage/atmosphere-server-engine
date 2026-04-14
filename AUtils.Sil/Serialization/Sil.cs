using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AUtils.Sil;

/// <summary>
/// Simple binary serializer
/// </summary>
public static partial class Sil
{
    private class Serializer
    {
        public ushort Index { get; init; }
        public Type Type { get; init; }
        public Delegate SerializeMethod { get; set; }
        public Func<ReadOnlyMemory<byte>, ValueTuple<object, int>> DeserializeMethod { get; set; }
    }

    private static ConcurrentDictionary<ushort, Serializer> SerializersByIndex { get; } = new();

    private static ConcurrentDictionary<Type, Serializer> SerializersByTypes { get; } = new();

    static Sil()
    {
        RegisterSystemTypes();
        ValidateSilIndexes();
    }

    /// <summary>
    /// Register new type for serialization
    /// </summary>
    public static void Register<T>(ushort index) => Register(index, typeof(T));

    /// <summary>
    /// Register new type for serialization
    /// </summary>
    public static void Register(ushort index, Type type)
    {
        if (index < 100)
            throw new SilException("0-99 indexes are reserved and can't be used");

        if (type.IsAbstract || type.IsInterface || type.IsGenericType)
            throw new SilException($"Unsupported type {type.FullName}");

        if (SerializersByIndex.ContainsKey(index) && SerializersByIndex[index].Type != type)
            throw new SilException($"Type {type.FullName} is already registered");

        var serializer = new Serializer {Type = type, Index = index};
        SerializersByIndex.TryAdd(index, serializer);
        SerializersByTypes.TryAdd(type, serializer);
    }

    internal static bool IsRegistered(Type type, out ushort index)
    {
        if (type == null) 
            throw new ArgumentNullException(nameof(type));

        if (SerializersByTypes.TryGetValue(type, out var serializer))
        {
            index = serializer.Index;
            return true;
        }

        var attrIndex = type.GetCustomAttribute<SilAttribute>()?.Index;
        index = attrIndex ?? default;
        return attrIndex.HasValue;
    }

    internal static void IsRegisteredOrThrow(Type type)
    {
        if (!IsRegistered(type, out _))
            throw new SilException(
                $"To use serialization of {type.FullName ?? type.Name} add {nameof(SilAttribute)} with unique index to {(type.IsClass ? "class" : "struct")}");
    }
    
    internal static bool IsSystemType(Type type) => type.Namespace?.StartsWith("System") == true;

    /// <summary>
    /// Check <see cref="SilAttribute.Index"/> values
    /// </summary>
    private static void ValidateSilIndexes()
    {
        var serializeTypes = Types.All
            .Where(x => x.GetCustomAttribute<SilAttribute>() != null)
            .Select(x => (x.GetCustomAttribute<SilAttribute>()?.Index ?? default, x))
            .ToArray();

        var groupedTypes = serializeTypes.GroupBy(x => x.Item1).ToArray();
        if (groupedTypes.Length != serializeTypes.Length)
        {
            var duplicatedIndexes = string.Join(',', groupedTypes
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray());

            throw new SilException($"There are duplicated indexes: {duplicatedIndexes}");
        }

        if (groupedTypes.Any(x => x.Key < 100))
            throw new SilException("0-99 indexes are reserved and can't be used");
    }
}