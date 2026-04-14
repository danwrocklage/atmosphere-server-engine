using System;
using System.Collections.Generic;

namespace AUtils.Sil;

public static partial class Sil
{
    private static void AddSerializer<T>(ushort index, Action<T, Memory<byte>> serialize,
        Func<ReadOnlyMemory<byte>, ValueTuple<object, int>> deserialize)
    {
        var serializer = new Serializer
        {
            Index = index, 
            Type = typeof(T), 
            SerializeMethod = serialize,
            DeserializeMethod = deserialize
        };
        SerializersByIndex.TryAdd(index, serializer);
        SerializersByTypes.TryAdd(typeof(T), serializer);
    }
    
    private static void RegisterSystemTypes()
    {
        AddSerializer(0, 
            new Action<object, Memory<byte>>((_, _) => throw new SilException("Index of 0 can't be used")),
            _ => throw new SilException("Index of 0 can't be used"));
        
        AddSerializer(1, 
            new Action<bool, Memory<byte>>((x, buffer) =>
                {
                    buffer.Span[0] = 0; // Start
                    BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 1);
                    buffer.Span[3] = (byte) (x ? 1 : 0);
                    buffer.Span[4] = 1; // End
                }),
            bytes => new ValueTuple<object, int>(bytes.Span[0] == 1, 1));
        
        AddSerializer(2, 
            new Action<byte, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 2);
                buffer.Span[3] = x;
                buffer.Span[4] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(bytes.Span[0], 1));
        
        AddSerializer(3, 
            new Action<sbyte, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 3);
                buffer.Span[3] = (byte) x;
                buffer.Span[4] = 1; // End
            }),
            bytes => new ValueTuple<object, int>((sbyte) bytes.Span[0], 1));
        
        AddSerializer(4, 
            new Action<short, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 4);
                BitConverter.TryWriteBytes(buffer.Span[3..], x);
                buffer.Span[5] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(BitConverter.ToInt16(bytes.Span[..2]), 2));
        
        AddSerializer(5, 
            new Action<ushort, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 5);
                BitConverter.TryWriteBytes(buffer.Span[3..], x);
                buffer.Span[5] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(BitConverter.ToUInt16(bytes.Span[..2]), 2));
        
        AddSerializer(6, 
            new Action<Half, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 6);
                BitConverter.TryWriteBytes(buffer.Span[3..], x);
                buffer.Span[5] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(BitConverter.ToHalf(bytes.Span[..2]), 2));
        
        AddSerializer(7, 
            new Action<char, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 7);
                BitConverter.TryWriteBytes(buffer.Span[3..], x);
                buffer.Span[5] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(BitConverter.ToChar(bytes.Span[..2]), 2));
        
        AddSerializer(8, 
            new Action<uint, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 8);
                BitConverter.TryWriteBytes(buffer.Span[3..], x);
                buffer.Span[7] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(BitConverter.ToUInt32(bytes.Span[..4]), 4));
        
        AddSerializer(9, 
            new Action<float, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 9);
                BitConverter.TryWriteBytes(buffer.Span[3..], x);
                buffer.Span[7] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(BitConverter.ToSingle(bytes.Span[..4]), 4));
        
        AddSerializer(10, 
            new Action<int, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 10);
                BitConverter.TryWriteBytes(buffer.Span[3..], x);
                buffer.Span[7] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(BitConverter.ToInt32(bytes.Span[..4]), 4));
        
        AddSerializer(11, 
            new Action<Index, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 11);
                BitConverter.TryWriteBytes(buffer.Span[3..], x.Value);
                buffer.Span[7] = (byte) (x.IsFromEnd ? 1 : 0);
                buffer.Span[8] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(new Index(BitConverter.ToInt32(bytes.Span[..4]), bytes.Span[4] == 1), 5));

        // 12 - DateOnly
        
        AddSerializer(12, 
            new Action<DateOnly, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 12);
                BitConverter.TryWriteBytes(buffer.Span[3..], x.DayNumber);
                buffer.Span[7] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(DateOnly.FromDayNumber(BitConverter.ToInt32(bytes.Span[..4])), 4));
        
        AddSerializer(13, 
            new Action<long, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 13);
                BitConverter.TryWriteBytes(buffer.Span[3..], x);
                buffer.Span[11] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(BitConverter.ToInt64(bytes.Span[..8]), 8));
        
        AddSerializer(14, 
            new Action<ulong, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 14);
                BitConverter.TryWriteBytes(buffer.Span[3..], x);
                buffer.Span[11] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(BitConverter.ToUInt64(bytes.Span[..8]), 8));
        
        AddSerializer(15, 
            new Action<double, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 15);
                BitConverter.TryWriteBytes(buffer.Span[3..], x);
                buffer.Span[11] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(BitConverter.ToDouble(bytes.Span[..8]), 8));
        
        // 16 DateTime
        AddSerializer(17, 
            new Action<TimeSpan, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 17);
                BitConverter.TryWriteBytes(buffer.Span[3..], x.Ticks);
                buffer.Span[11] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(new TimeSpan(BitConverter.ToInt64(bytes.Span[..8])), 8));
        
        AddSerializer(18, 
            new Action<TimeOnly, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 18);
                BitConverter.TryWriteBytes(buffer.Span[3..], x.Ticks);
                buffer.Span[11] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(new TimeOnly(BitConverter.ToInt64(bytes.Span[..8])), 8));
        
        AddSerializer(19, 
            new Action<Range, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 19);
                BitConverter.TryWriteBytes(buffer.Span[3..], x.Start.Value);
                buffer.Span[7] = (byte) (x.Start.IsFromEnd ? 1 : 0);
                BitConverter.TryWriteBytes(buffer.Span[8..], x.End.Value);
                buffer.Span[12] = (byte) (x.End.IsFromEnd ? 1 : 0);
                buffer.Span[13] = 1; // End
            }),
            bytes =>
            {
                var start = new Index(BitConverter.ToInt32(bytes.Span[..4]), bytes.Span[4] == 1);
                var end = new Index(BitConverter.ToInt32(bytes.Span.Slice(5, 4)), bytes.Span[9] == 1);
                return new ValueTuple<object, int>(new Range(start, end), 10);
            });
        
        AddSerializer(20, 
            new Action<Guid, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 20);
                x.TryWriteBytes(buffer.Slice(3, 16).Span);
                buffer.Span[19] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(new Guid(bytes.Span.Slice(0, 16)), 16));
        
        AddSerializer(21, 
            new Action<decimal, Memory<byte>>((x, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 21);
                var bits = decimal.GetBits(x);
                buffer.Span[3] = (byte) bits[0];
                buffer.Span[4] = (byte) (bits[0] >> 8);
                buffer.Span[5] = (byte) (bits[0] >> 16);
                buffer.Span[6] = (byte) (bits[0] >> 24);

                buffer.Span[7] = (byte) bits[1];
                buffer.Span[8] = (byte) (bits[1] >> 8);
                buffer.Span[9] = (byte) (bits[1] >> 16);
                buffer.Span[10] = (byte) (bits[1] >> 24);

                buffer.Span[11] = (byte) bits[2];
                buffer.Span[12] = (byte) (bits[2] >> 8);
                buffer.Span[13] = (byte) (bits[2] >> 16);
                buffer.Span[14] = (byte) (bits[2] >> 24);

                buffer.Span[15] = (byte) bits[3];
                buffer.Span[16] = (byte) (bits[3] >> 8);
                buffer.Span[17] = (byte) (bits[3] >> 16);
                buffer.Span[18] = (byte) (bits[3] >> 24);
                buffer.Span[19] = 1; // End
            }),
            bytes => new ValueTuple<object, int>(Generator.DecimalReadBytes(bytes), 16));

        AddSerializer(22, 
            new Action<byte[], Memory<byte>>((data, buffer) =>
            {
                if (data.Length > ushort.MaxValue)
                    throw new SilException("Serialization data is too large");
        
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 22);
                BitConverter.TryWriteBytes(buffer.Span[2..], Convert.ToUInt16(data.Length));
                for (var i = 0; i < data.Length; i++)
                    buffer.Span[i + 5] = data[i];
                buffer.Span[data.Length + 5] = 1; // End
            }),
            bytes =>
            {
                var size = BitConverter.ToUInt16(bytes.Span[..2]);
                return new ValueTuple<object, int>(bytes.Span.Slice(2, size).ToArray(), size + 2);
            });
        
        AddSerializer(23, 
            new Action<string, Memory<byte>>((data, buffer) =>
            {
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 23);
                var written = Generator.StringWriteBytes(data, buffer[3..]);
                buffer.Span[written + 3] = 1; //End 
            }),
            bytes =>
            {
                var size = BitConverter.ToUInt16(bytes.Span[..2]);
                var result = Generator.StringReadBytes(bytes.Slice(2, size));
                return new ValueTuple<object, int>(result.Result, result.Read);
            });
        
        AddSerializer(24, 
            new Action<Memory<byte>, Memory<byte>>((data, buffer) =>
            {
                if (data.Length > ushort.MaxValue)
                    throw new SilException("Serialization data is too large");
        
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 22);
                BitConverter.TryWriteBytes(buffer.Span[2..], Convert.ToUInt16(data.Length));
                for (var i = 0; i < data.Length; i++)
                    buffer.Span[i + 5] = data.Span[i];
                buffer.Span[data.Length + 5] = 1; // End
            }),
            bytes =>
            {
                var size = BitConverter.ToUInt16(bytes.Span[..2]);
                return new ValueTuple<object, int>(new Memory<byte>(bytes.Slice(2, size).ToArray()), size + 2);
            });
        
        AddSerializer(25, 
            new Action<ReadOnlyMemory<byte>, Memory<byte>>((data, buffer) =>
            {
                if (data.Length > ushort.MaxValue)
                    throw new SilException("Serialization data is too large");
        
                buffer.Span[0] = 0; // Start
                BitConverter.TryWriteBytes(buffer.Span[1..], (ushort) 22);
                BitConverter.TryWriteBytes(buffer.Span[2..], Convert.ToUInt16(data.Length));
                for (var i = 0; i < data.Length; i++)
                    buffer.Span[i + 5] = data.Span[i];
                buffer.Span[data.Length + 5] = 1; // End
            }),
            bytes =>
            {
                var size = BitConverter.ToUInt16(bytes.Span[..2]);
                return new ValueTuple<object, int>(bytes.Slice(2, size), size + 2);
            });
    }
}