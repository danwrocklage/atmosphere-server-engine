using System;
using System.Linq;
using System.Reflection;

namespace AUtils.Sil;

public static partial class Sil
{
    /// <summary>
    /// Binary deserialization
    /// </summary>
    public static (object Result, Type ResultType) Deserialize(ReadOnlyMemory<byte> buffer)
    {
        var (result, _) = DeserializeInternal(buffer);
        return (result, result.GetType());
    }
    
    private static (object Result, int Offset) DeserializeInternal(ReadOnlyMemory<byte> buffer)
    {
        if (buffer.Span[0] != 0)
            throw new SilException($"Expected 0, but got {buffer.Span[0].ToString()} in the start of deserialization");

        var index = BitConverter.ToUInt16(buffer.Slice(1, 2).Span);
        var serializer = GetSerializer(index);

        serializer.DeserializeMethod ??= Generator.GenerateDeserialize(serializer.Type);

        try
        {
            return serializer.DeserializeMethod(buffer[3..]);
        }
        catch (Exception e)
        {
            throw new SilException("Fail serialize", e);
        }
    }
    
    private static Serializer GetSerializer(ushort index)
    {
        if (SerializersByIndex.TryGetValue(index, out var serializer)) 
            return serializer;
        
        var type = Types.All.FirstOrDefault(x => x.GetCustomAttribute<SilAttribute>()?.Index == index);
        if(type == null)
            throw new SilException($"Can't find type with index = {index.ToString()}. Check that type with this index exists and all assemblies are loaded");
     
        Register(type.GetCustomAttribute<SilAttribute>()!.Index!.Value, type);
        
        return SerializersByIndex[index];
    }
    
    internal static object SubDeserialize(ReadOnlyMemory<byte> buffer) => DeserializeInternal(buffer).Result;
}