using System;
using System.Collections.Generic;

namespace AUtils.Sil;

internal static partial class Generator
{
    private static int SerializeDictionary<TKey, TValue>(Memory<byte> buffer, IDictionary<TKey, TValue> dictionary)
    {
        var valueBuffer = buffer[2..];
        ushort totalSize = 0;
        foreach (var entry in dictionary)
        {
            var size = Sil.OutputSize(entry.Key);
            BitConverter.TryWriteBytes(valueBuffer.Span, size);
            valueBuffer = valueBuffer[2..];
            Sil.Serialize(entry.Key, valueBuffer);
            valueBuffer = valueBuffer[size..];
            totalSize += (ushort) (size + 2);
            
            size = Sil.OutputSize(entry.Value);
            BitConverter.TryWriteBytes(valueBuffer.Span, size);
            valueBuffer = valueBuffer[2..];
            Sil.Serialize(entry.Value, valueBuffer);
            valueBuffer = valueBuffer[size..];
            totalSize += (ushort) (size + 2);
        }

        BitConverter.TryWriteBytes(buffer.Span, totalSize);
        
        return totalSize + 2;
    }

    private static int SerializeEnumerable<T>(Memory<byte> buffer, IEnumerable<T> collection)
    {
        var valueBuffer = buffer[2..];
        ushort totalSize = 0;
        foreach (var entry in collection)
        {
            var size = Sil.OutputSize(entry);
            BitConverter.TryWriteBytes(valueBuffer.Span, size);
            valueBuffer = valueBuffer[2..];
            Sil.Serialize(entry, valueBuffer);
            valueBuffer = valueBuffer[size..];
            totalSize += (ushort) (size + 2);
        }

        BitConverter.TryWriteBytes(buffer.Span, totalSize);
        
        return totalSize + 2;
    }
}