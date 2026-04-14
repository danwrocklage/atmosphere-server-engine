using System;
using System.Reflection;

namespace AUtils.Sil;

public static partial class Sil
{
    /// <summary>
    /// Binary serialization
    /// </summary>
    public static void Serialize<T>(T instance, Memory<byte> destination)
    {
        if (instance == null)
            return;

        var type = typeof(T) == typeof(object) ? instance.GetType() : typeof(T);
        var serializer = GetSerializer(type);

        serializer.SerializeMethod ??= Generator.GenerateSerialize(serializer.Type);
        if (serializer.Type == typeof(T))
            ((Action<T, Memory<byte>>) serializer.SerializeMethod)(instance, destination);
        else
            serializer.SerializeMethod.DynamicInvoke(instance, destination);
    }
    
    private static Serializer GetSerializer(Type type)
    {
        if (SerializersByTypes.TryGetValue(type, out var serializer)) 
            return serializer;
        
        var attr = type.GetCustomAttribute<SilAttribute>();
        if(attr?.Index == null)
            throw new SilException($"To use serialization of {type.FullName ?? type.Name} add {nameof(SilAttribute)} with unique index to {(type.IsClass ? "class" : "struct")}");
               
        Register(attr.Index.Value, type);
        
        return SerializersByTypes[type];
    }
}