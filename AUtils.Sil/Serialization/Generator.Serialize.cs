using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sigil.NonGeneric;

namespace AUtils.Sil;

internal static partial class Generator
{
    private const string SPAN_LOCAL = "spanLocal";
    private const string MEMORY_LOCAL = "memoryLocal";
    private const string GUID_LOCAL = "guidLocal";
    
    private static readonly MethodInfo sSerialization = typeof(Sil).GetMethods().Single(x =>
        x.IsGenericMethod && x.Name == nameof(Sil.Serialize) && x.GetParameters().Length == 2);
    private static readonly MethodInfo sMemorySpan = typeof(Memory<byte>).GetProperty(nameof(Memory<byte>.Span))!.GetGetMethod();
    private static readonly MethodInfo sSpanItem = typeof(Span<byte>).GetProperty("Item")!.GetGetMethod();
    
    internal static PropertyInfo[] GetSerializingProperties(Type type) =>
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => 
                !x.GetCustomAttributes<SilIgnoreAttribute>().Any() && 
                x.CanRead && x.CanWrite)
            .ToArray();
    
    public static Delegate GenerateSerialize(Type type)
    {
        var il = Emit.NewDynamicMethod(typeof(void), new[] { type, typeof(Memory<byte>) }, $"SerializerOf{type.FullName?.Replace('.', '_')}");

        var props = GetSerializingProperties(type);
        var isDynamicBufferSize = IsDynamicBufferSize(props);

        il.DefineLabel("return");
        il.DeclareLocal(typeof(Span<byte>), SPAN_LOCAL);
        il.DeclareLocal(typeof(Memory<byte>), MEMORY_LOCAL);
        il.DeclareLocal(typeof(Guid), GUID_LOCAL);

        DirectWriteByte(il, 
            () => il.LoadConstant(0), 
            () => il.LoadConstant(0));
        WriteTypeIndex(type, il);

        if (!isDynamicBufferSize)
            ValueTypeGenerator(type, props, il);
        else
            RefTypeGenerator(type, il, props);

        il.MarkLabel("return");
        il.Return();

        return il.CreateDelegate(typeof(Action<,>).MakeGenericType(type, typeof(Memory<byte>)));
    }

    private static void RefTypeGenerator(Type type, Emit il, PropertyInfo[] props)
    {
        il.LoadConstant(3);
        il.DeclareLocal<int>("offset");
        il.StoreLocal("offset");
        
        foreach (var prop in props)
        {
            il.DefineLabel($"prop{prop.Name}End");
            
            if (!prop.PropertyType.IsValueType || IsNullable(prop))
            {
                il.DefineLabel($"prop{prop.Name}IsNull");
                il.DefineLabel($"prop{prop.Name}IsNotNull");

                il.LoadArgument(0);
                il.Call(prop.GetMethod);

                if (IsNullable(prop))
                {
                    var nullablePropLocal = il.DeclareLocal(prop.PropertyType);
                    il.StoreLocal(nullablePropLocal);
                    il.LoadLocalAddress(nullablePropLocal);
                    il.Call(prop.PropertyType.GetProperty(nameof(Nullable<bool>.HasValue))!.GetGetMethod());
                    il.LoadConstant(false);
                }
                else
                    il.LoadNull();
                
                il.BranchIfEqual($"prop{prop.Name}IsNull");
                il.Branch($"prop{prop.Name}IsNotNull");

                il.MarkLabel($"prop{prop.Name}IsNull");

                for (var i = 0; i < 2; i++)
                {
                    DirectWriteByte(il, () => il.LoadLocal("offset"), () => il.LoadConstant(0));
                    IncrementOffset(il);
                }
                
                il.Branch($"prop{prop.Name}End");

                il.MarkLabel($"prop{prop.Name}IsNotNull");
            }
            
            WriteProperty(prop, il, type);
            
            il.MarkLabel($"prop{prop.Name}End");
        }
        
        DirectWriteByte(il, () => il.LoadLocal("offset"), () => il.LoadConstant(1));
    }

    private static void IncrementOffset(Emit il, int value = 1)
    {
        il.LoadLocal("offset");
        il.LoadConstant(value);
        il.Add();
        il.StoreLocal("offset");
    }

    private static void ValueTypeGenerator(Type type, PropertyInfo[] props, Emit il)
    {
        var offset = 3;

        foreach (var prop in props)
        {
            var propertyType = prop.PropertyType.IsEnum ? Enum.GetUnderlyingType(prop.PropertyType) : prop.PropertyType;
            var propSize = (short) GetSizeOf(propertyType);

            //  arg_0.Span[offset] = size
            var propSizeBytes = BitConverter.GetBytes(propSize);
            DirectWriteByte(il, 
                () => il.LoadConstant(offset), 
                () => il.LoadConstant(propSizeBytes[0]));
            DirectWriteByte(il, 
                () => il.LoadConstant(offset + 1), 
                () => il.LoadConstant(propSizeBytes[1]));
            offset += 2;

            if (propertyType == typeof(byte))
                DirectWriteByte(il, () => il.LoadConstant(offset), () => LoadProperty(il, type, prop));
            else if (propertyType == typeof(Guid))
            {
                LoadProperty(il, type, prop);
                il.IsInstance(propertyType);
                il.StoreLocal(GUID_LOCAL);
                il.LoadLocalAddress(GUID_LOCAL);
                    
                il.LoadArgumentAddress(1);
                il.LoadConstant(offset);
                il.Call(typeof(Memory<byte>).GetMethod(nameof(Memory<byte>.Slice), new[] {typeof(int)}));
                il.StoreLocal(MEMORY_LOCAL);
                il.LoadLocalAddress(MEMORY_LOCAL);
                il.Call(sMemorySpan);

                il.Call(typeof(Guid).GetMethod(nameof(Guid.TryWriteBytes), new[] {typeof(Span<byte>)}));
                il.Pop();
            }
            else if (Sil.IsSystemType(propertyType))
            {
                il.LoadArgumentAddress(1);
                il.LoadConstant(offset);
                il.Call(typeof(Memory<byte>).GetMethod(nameof(Memory<byte>.Slice), new[] {typeof(int)}));
                il.StoreLocal(MEMORY_LOCAL);
                il.LoadLocalAddress(MEMORY_LOCAL);
                il.Call(sMemorySpan);

                LoadProperty(il, type, prop);
                il.IsInstance(propertyType);
                il.Call(typeof(BitConverter).GetMethod(nameof(BitConverter.TryWriteBytes), new[] {typeof(Span<byte>), propertyType}));
                il.Pop();
            }
            else
            {
                LoadProperty(il, type, prop);
                    
                il.LoadArgumentAddress(1);
                il.LoadConstant(offset);
                il.Call(typeof(Memory<byte>).GetMethod(nameof(Memory<byte>.Slice), new[] {typeof(int)}));
                    
                il.Call(sSerialization.MakeGenericMethod(propertyType));
            }

            offset += propSize;
        }

        DirectWriteByte(il, 
            () => il.LoadConstant(offset), 
            () => il.LoadConstant(1));
    }

    private static int GetSizeOf(Type propertyType)
    {
        if (propertyType == typeof(bool))
            return 1;

        if (Sil.IsSystemType(propertyType))
            return Marshal.SizeOf(propertyType);
        
        var props = GetSerializingProperties(propertyType);
        var size = 4; // start byte + type bytes(2) + end byte
        
        for (int i = 0; i < props.Length; i++)
        {
            var propSize = GetSizeOf(props[i].PropertyType);
            size += propSize + 2;
        }

        return size;
    }
    
    private static bool IsDynamicBufferSize(PropertyInfo[] props)
    {
        foreach (var x in props)
        {
            if (!x.PropertyType.IsValueType || IsNullable(x))
                return true;
            
            if (!Sil.IsSystemType(x.PropertyType) && IsDynamicBufferSize(GetSerializingProperties(x.PropertyType)))
                return true;
        }

        return false;
    }
    
    private static void WriteProperty(PropertyInfo prop, Emit il, Type type)
    {
        var propertyType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        if (propertyType.IsEnum)
            propertyType = Enum.GetUnderlyingType(propertyType);

        if (propertyType == typeof(byte) || propertyType == typeof(sbyte))
        {
            DirectWriteByte(il, () => il.LoadLocal("offset"), () => il.LoadConstant(1));
            IncrementOffset(il);
            DirectWriteByte(il, () => il.LoadLocal("offset"), () => il.LoadConstant(0));
            IncrementOffset(il);
            DirectWriteByte(il, () => il.LoadLocal("offset"), () => LoadProperty(il, type, prop));
            IncrementOffset(il);
            return;
        }

        if (propertyType == typeof(Guid))
        {
            DirectWriteByte(il, () => il.LoadLocal("offset"), () => il.LoadConstant(16));
            IncrementOffset(il);
            DirectWriteByte(il, () => il.LoadLocal("offset"), () => il.LoadConstant(0));
            IncrementOffset(il);
            
            LoadProperty(il, type, prop);
            il.StoreLocal(GUID_LOCAL);
            il.LoadLocalAddress(GUID_LOCAL);
            
            il.LoadArgumentAddress(1);
            il.LoadLocal("offset");
            il.Call(typeof(Memory<byte>).GetMethod(nameof(Memory<byte>.Slice), new[] {typeof(int)}));
            il.StoreLocal(MEMORY_LOCAL);
            il.LoadLocalAddress(MEMORY_LOCAL);
            il.Call(sMemorySpan);

            il.Call(typeof(Guid).GetMethod(nameof(Guid.TryWriteBytes), new[] {typeof(Span<byte>)}));
            il.Pop();
            
            IncrementOffset(il, 16);
            return;
        }
        
        if (propertyType == typeof(decimal))
        {
            DirectWriteByte(il, () => il.LoadLocal("offset"), () => il.LoadConstant(16));
            IncrementOffset(il);
            DirectWriteByte(il, () => il.LoadLocal("offset"), () => il.LoadConstant(0));
            IncrementOffset(il);
            
            LoadProperty(il, type, prop);
            il.StoreLocal(GUID_LOCAL);
            il.LoadLocalAddress(GUID_LOCAL);
            
            il.LoadArgumentAddress(1);
            il.LoadLocal("offset");
            il.Call(typeof(Memory<byte>).GetMethod(nameof(Memory<byte>.Slice), new[] {typeof(int)}));

            il.Call(typeof(Generator).GetMethod(nameof(DecimalWriteBytes), new[] {typeof(decimal), typeof(Memory<byte>)}));
            il.Pop();
            
            IncrementOffset(il, 16);
            return;
        }
        
        if (propertyType == typeof(string))
        {
            LoadProperty(il, type, prop);
            
            il.LoadArgumentAddress(1);
            il.LoadLocal("offset");
            il.Call(typeof(Memory<byte>).GetMethod(nameof(Memory<byte>.Slice), new[] {typeof(int)}));
            
            il.Call(typeof(Generator).GetMethod(nameof(StringWriteBytes), BindingFlags.NonPublic | BindingFlags.Static));
            il.LoadLocal("offset");
            il.Add();
            il.StoreLocal("offset");
            
            return;
        }
        
        if (propertyType == typeof(DateTime))
        {
            DirectWriteByte(il, () => il.LoadLocal("offset"), () => il.LoadConstant(8));
            IncrementOffset(il);
            DirectWriteByte(il, () => il.LoadLocal("offset"), () => il.LoadConstant(0));
            IncrementOffset(il);
            
            il.DeclareLocal<DateTime>(out var datetimeLocal);
            
            il.LoadArgumentAddress(1);
            il.LoadLocal("offset");
            il.Call(typeof(Memory<byte>).GetMethod(nameof(Memory<byte>.Slice), new[] {typeof(int)}));
            il.StoreLocal(MEMORY_LOCAL);
            il.LoadLocalAddress(MEMORY_LOCAL);
            il.Call(sMemorySpan);
            
            LoadProperty(il, type, prop);
            il.StoreLocal(datetimeLocal);
            il.LoadLocalAddress(datetimeLocal);
            
            il.Call(typeof(DateTime).GetMethod(nameof(DateTime.ToBinary)));
            il.Call(typeof(BitConverter).GetMethod(nameof(BitConverter.TryWriteBytes), new[] {typeof(Span<byte>), typeof(long)}));
            il.Pop();

            IncrementOffset(il, 8);
            return;
        }
        
        if (Sil.IsSystemType(propertyType) && propertyType.IsValueType)
        {
            var size = Sil.GetValueTypeSize(propertyType);
            var sizeBytes = BitConverter.GetBytes(size);
            DirectWriteByte(il, () => il.LoadLocal("offset"), () => il.LoadConstant(sizeBytes[0]));
            IncrementOffset(il);
            DirectWriteByte(il, () => il.LoadLocal("offset"), () => il.LoadConstant(sizeBytes[1]));
            IncrementOffset(il);
            
            il.LoadArgumentAddress(1);
            il.LoadLocal("offset");
            il.Call(typeof(Memory<byte>).GetMethod(nameof(Memory<byte>.Slice), new[] {typeof(int)}));
            il.StoreLocal(MEMORY_LOCAL);
            il.LoadLocalAddress(MEMORY_LOCAL);
            il.Call(sMemorySpan);
            
            LoadProperty(il, type, prop);
            
            il.Call(typeof(BitConverter).GetMethod(nameof(BitConverter.TryWriteBytes), new[] {typeof(Span<byte>), propertyType}));
            il.Pop();
            
            IncrementOffset(il, size);
            return;
        }

        if (propertyType == typeof(byte[]))
        {
            LoadProperty(il, type, prop);
            
            il.LoadArgumentAddress(1);
            il.LoadLocal("offset");
            il.Call(typeof(Memory<byte>).GetMethod(nameof(Memory<byte>.Slice), new[] {typeof(int)}));
            
            il.Call(typeof(Generator).GetMethod(nameof(ByteArrayWriteBytes), BindingFlags.NonPublic | BindingFlags.Static));
            il.LoadLocal("offset");
            il.Add();
            il.StoreLocal("offset");
            
            return;
        }

        var dictionaryProp = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IDictionary<,>) ? 
            propertyType : propertyType.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        if (dictionaryProp != null)
        {
            il.DeclareLocal(propertyType, out var customPropLocal);

            il.LoadArgumentAddress(1);
            il.LoadLocal("offset");
            il.Call(typeof(Memory<byte>).GetMethod(nameof(Memory<byte>.Slice), new[] {typeof(int)}));

            LoadProperty(il, type, prop);
            il.StoreLocal(customPropLocal);
            il.LoadLocal(customPropLocal);
            
            il.Call(typeof(Generator)
                .GetMethod(nameof(SerializeDictionary), BindingFlags.Static | BindingFlags.NonPublic)?
                .MakeGenericMethod(dictionaryProp.GetGenericArguments()));
            il.LoadLocal("offset");
            il.Add();
            il.StoreLocal("offset");
            
            return;
        }
        
        
        var enumerableProp = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ? 
            propertyType : propertyType.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumerableProp != null)
        {
            il.DeclareLocal(propertyType, out var customPropLocal);

            il.LoadArgumentAddress(1);
            il.LoadLocal("offset");
            il.Call(typeof(Memory<byte>).GetMethod(nameof(Memory<byte>.Slice), new[] {typeof(int)}));

            LoadProperty(il, type, prop);
            il.StoreLocal(customPropLocal);
            il.LoadLocal(customPropLocal);
            
            il.Call(typeof(Generator)
                .GetMethod(nameof(SerializeEnumerable), BindingFlags.Static | BindingFlags.NonPublic)?
                .MakeGenericMethod(enumerableProp.GetGenericArguments()));
            il.LoadLocal("offset");
            il.Add();
            il.StoreLocal("offset");
        }
        else
        {
            il.DeclareLocal(propertyType, out var customPropLocal);
            il.DeclareLocal<ushort>(out var customPropSizeLocal);
            
            il.LoadArgumentAddress(1);
            il.LoadLocal("offset");
            il.Call(typeof(Memory<byte>).GetMethod(nameof(Memory<byte>.Slice), new[] {typeof(int)}));
            il.StoreLocal(MEMORY_LOCAL);
            il.LoadLocalAddress(MEMORY_LOCAL);
            il.Call(sMemorySpan);
            
            LoadProperty(il, type, prop);
            il.StoreLocal(customPropLocal);
            il.LoadLocal(customPropLocal);

            il.Call(typeof(Sil).GetMethod(nameof(Sil.OutputSize))?.MakeGenericMethod(propertyType));
            il.StoreLocal(customPropSizeLocal);
            il.LoadLocal(customPropSizeLocal);
            il.Call(typeof(BitConverter).GetMethod(nameof(BitConverter.TryWriteBytes), new[] {typeof(Span<byte>), typeof(ushort)}));
            il.Pop();
            
            IncrementOffset(il, 2);
            
            LoadProperty(il, type, prop);
            il.StoreLocal(customPropLocal);
            il.LoadLocal(customPropLocal);
            
            il.LoadArgumentAddress(1);
            il.LoadLocal("offset");
            il.Call(typeof(Memory<byte>).GetMethod(nameof(Memory<byte>.Slice), new[] {typeof(int)}));
            
            il.Call(sSerialization.MakeGenericMethod(propertyType));
            
            il.LoadLocal("offset");
            il.LoadLocal(customPropSizeLocal);
            il.Add();
            il.StoreLocal("offset");
        }
    }

    private static bool IsNullable(PropertyInfo prop) => prop.PropertyType.FullName?.StartsWith("System.Nullable") == true;

    private static void LoadProperty(Emit il, Type type, PropertyInfo prop)
    {
        if (type.IsValueType)
            il.LoadArgumentAddress(0);
        else
            il.LoadArgument(0);
        il.Call(prop.GetGetMethod());
        if (IsNullable(prop))
        {
            var t = prop.GetGetMethod()?.ReturnType;
            il.Box(t);
            il.Unbox(t);
            il.Call(t?.GetProperty("Value")?.GetGetMethod());
        }
    }
    
    private static void WriteTypeIndex(Type type, Emit il)
    {
        if (!Sil.IsRegistered(type, out var typeIndex))
            throw new SilException($"{type.FullName} requires {nameof(SilAttribute)}");
        
        var typeBytes = BitConverter.GetBytes(typeIndex);
        
        DirectWriteByte(il, 
            () => il.LoadConstant(1), 
            () => il.LoadConstant(typeBytes[0]));
        DirectWriteByte(il, 
            () => il.LoadConstant(2), 
            () => il.LoadConstant(typeBytes[1]));
    }

    private static void DirectWriteByte(Emit il, Action offset, Action value)
    {
        il.LoadArgumentAddress(1);
        il.Call(sMemorySpan);
        il.StoreLocal(SPAN_LOCAL);
        il.LoadLocalAddress(SPAN_LOCAL);
        offset();
        il.Call(sSpanItem);
        value();
        il.StoreIndirect<byte>();
    }
    
    internal static unsafe int ByteArrayWriteBytes(byte[] source, Memory<byte> destination)
    {
        if (source == null)
        {
            destination.Span[0] = 0;
            destination.Span[1] = 0;
            return 2;
        }

        var len = source.Length;
        Unsafe.As<byte, int>(ref destination.Span[0]) = len;
        
        fixed (byte* pointer = source)
        {
            var bytesDest = destination[2..];
            Buffer.MemoryCopy(pointer, bytesDest.Pin().Pointer, bytesDest.Length, len);
        }
        return len + 2;
    }
    
    internal static unsafe int StringWriteBytes(string str, Memory<byte> destination)
    {
        if (str == null)
        {
            destination.Span[0] = 0;
            destination.Span[1] = 0;
            return 2;
        }

        if (str == string.Empty)
        {
            destination.Span[0] = 1;
            destination.Span[1] = 0;
            destination.Span[2] = 0;
            return 3;
        }

        var len = str.Length * sizeof(char);
        Unsafe.As<byte, int>(ref destination.Span[0]) = len;
        
        fixed (char* pointer = &str.GetPinnableReference())
        {
            var strDest = destination[2..];
            Buffer.MemoryCopy(pointer, strDest.Pin().Pointer, strDest.Length, len);
        }

        return len + 2;
    }

    internal static int DecimalWriteBytes(decimal source, Memory<byte> destination)
    {
        var span = destination.Span;
        var bits = decimal.GetBits(source);
        span[3] = (byte) bits[0];
        span[4] = (byte) (bits[0] >> 8);
        span[5] = (byte) (bits[0] >> 16);
        span[6] = (byte) (bits[0] >> 24);

        span[7] = (byte) bits[1];
        span[8] = (byte) (bits[1] >> 8);
        span[9] = (byte) (bits[1] >> 16);
        span[10] = (byte) (bits[1] >> 24);

        span[11] = (byte) bits[2];
        span[12] = (byte) (bits[2] >> 8);
        span[13] = (byte) (bits[2] >> 16);
        span[14] = (byte) (bits[2] >> 24);

        span[15] = (byte) bits[3];
        span[16] = (byte) (bits[3] >> 8);
        span[17] = (byte) (bits[3] >> 16);
        span[18] = (byte) (bits[3] >> 24);

        return 16;
    }
}