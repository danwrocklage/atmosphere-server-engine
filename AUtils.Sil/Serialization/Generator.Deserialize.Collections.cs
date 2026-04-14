using System;
using System.Collections.Generic;

namespace AUtils.Sil;

internal static partial class Generator
{
    private static TCollection DeserializeDictionary<TKey, TValue, TCollection>(ReadOnlyMemory<byte> input) where TCollection : IDictionary<TKey, TValue>, new()
    {
        var i = 0;
        var result = new TCollection();
        while (i < input.Length)
        {
            var key = (TKey) GetObject(input[i..], out var read);
            i += read;
            var value = (TValue) GetObject(input[i..], out read);
            i += read;

            result.Add(key, value);
        }
        return result;
    }
    /*private static IReadOnlyDictionary<TKey, TValue> DeserializeReadOnlyDictionary<TKey, TValue>(ReadOnlyMemory<byte> input)
    {
        var i = 0;
        var result = new Dictionary<TKey, TValue>();
        while (i < input.Length)
        {
            result.Add((TKey) GetObject(ref i, input), (TValue) GetObject(ref i, input));
        }
        return result;
    }
    private static TCollection DeserializeList<TKey, TCollection>(ReadOnlyMemory<byte> input) where TCollection : ICollection<TKey>, new()
    {
        var i = 0;
        var result = new TCollection();
        while (i < input.Length)
        {
            result.Add((TKey) GetObject(ref i, input));
        }
        return result;
    }*/
        
    /*private static TKey[] DeserializeArray<TKey>(ReadOnlyMemory<byte> input)
    {
        var i = 0;
        var result = new List<TKey>();
        while (i < input.Length)
        {
            result.Add((TKey) GetObject(ref i, input));
        }
        return result.ToArray();
    }*/

    private static object GetObject(ReadOnlyMemory<byte> input, out int read)
    {
        var len = BitConverter.ToInt16(input.Span);
        read = len + 2;

        var source = input.Slice(2, len);
        
        if (source.Length == 4 &&
            source.Span[0] == 0 &&
            source.Span[1] == 0 &&
            source.Span[2] == 0 &&
            source.Span[3] == 0)
            return null;
        
        return len < 2 ? null : Sil.SubDeserialize(source);
    }

    private static IEnumerable<TValue> DeserializeEnumerable<TValue>(ReadOnlyMemory<byte> input)
    {
        var i = 0;
        while (i < input.Length)
        {
            yield return (TValue) GetObject(input[i..], out var read);
            i += read;
        }
    }

    private static TValue[] DeserializeArray<TValue>(ReadOnlyMemory<byte> input) =>
        DeserializeList<TValue>(input).ToArray();

    private static List<TValue> DeserializeList<TValue>(ReadOnlyMemory<byte> input)
    {
        var i = 0;
        var result = new List<TValue>();
        while (i < input.Length)
        {
            result.Add((TValue) GetObject(input[i..], out var read));
            i += read;
        }
        return result;
    }
    
    private static Queue<TValue> DeserializeQueue<TValue>(ReadOnlyMemory<byte> input)
    {
        var i = 0;
        var result = new Queue<TValue>();
        while (i < input.Length)
        {
            result.Enqueue((TValue) GetObject(input[i..], out var read));
            i += read;
        }
        return result;
    }

    private static Stack<TValue> DeserializeStack<TValue>(ReadOnlyMemory<byte> input)
    {
        var i = 0;
        var result = new Stack<TValue>();
        while (i < input.Length)
        {
            result.Push((TValue) GetObject(input[i..], out var read));
            i += read;
        }
        return result;
    }
    
    private static HashSet<TValue> DeserializeHashSet<TValue>(ReadOnlyMemory<byte> input)
    {
        var i = 0;
        var result = new HashSet<TValue>();
        while (i < input.Length)
        {
            result.Add((TValue) GetObject(input[i..], out var read));
            i += read;
        }
        return result;
    }
}