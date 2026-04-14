using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sigil;

namespace AUtils.Sil;

internal static partial class Generator
{
    private static readonly MethodInfo sReadOnlyMemorySpan = typeof(ReadOnlyMemory<byte>).GetProperty(nameof(ReadOnlyMemory<byte>.Span))!.GetGetMethod();
    private static readonly MethodInfo sReadOnlySpanItem = typeof(ReadOnlySpan<byte>).GetProperty("Item")!.GetGetMethod();

    public static Func<ReadOnlyMemory<byte>, ValueTuple<object, int>> GenerateDeserialize(Type type)
    {
        var il = Emit<Func<ReadOnlyMemory<byte>, ValueTuple<object, int>>>
            .NewDynamicMethod($"DeserializerOf{type.FullName?.Replace('.', '_')}");
        
        var props = GetSerializingProperties(type);

        il.DeclareLocal<ushort>("size");
        il.DeclareLocal(typeof(ReadOnlyMemory<byte>), MEMORY_LOCAL);
        il.DeclareLocal(typeof(ReadOnlySpan<byte>), SPAN_LOCAL);

        il.LoadConstant(0);
        il.DeclareLocal<int>("current");
        il.StoreLocal("current");
        
        il.Call(typeof(Activator).GetMethods().First(x =>
            x.IsGenericMethod &&
            x.Name == nameof(Activator.CreateInstance) &&
            x.GetParameters().Length == 0).MakeGenericMethod(type));
        il.DeclareLocal(type, "instance");
        il.StoreLocal("instance");
        
        foreach (var prop in props)
        {
            // size = BitConverter.ToUInt16(arg_0.Slice(current, 2));
            il.LoadArgumentAddress(0);
            il.LoadLocal("current");
            il.LoadConstant(2);
            il.Call(typeof(ReadOnlyMemory<byte>).GetMethod(nameof(ReadOnlyMemory<byte>.Slice), new[] {typeof(int), typeof(int)}));
            il.StoreLocal(MEMORY_LOCAL);
            il.LoadLocalAddress(MEMORY_LOCAL);
            il.Call(sReadOnlyMemorySpan);
            il.Call(typeof(BitConverter).GetMethod(nameof(BitConverter.ToUInt16), new[] { typeof(ReadOnlySpan<byte>) }));
            il.StoreLocal("size");
            
            // current += 2
            il.LoadConstant(2);
            il.LoadLocal("current");
            il.Add();
            il.StoreLocal("current");
            
            il.DefineLabel($"prop{prop.Name}End");

            if (!prop.PropertyType.IsValueType || IsNullable(prop))
            {
                il.DefineLabel($"prop{prop.Name}IsNull");
                il.DefineLabel($"prop{prop.Name}IsNotNull");

                il.LoadLocal("size");
                il.LoadConstant(0);
                il.BranchIfEqual($"prop{prop.Name}IsNull");
                il.Branch($"prop{prop.Name}IsNotNull");

                il.MarkLabel($"prop{prop.Name}IsNull");
                    
                il.LoadLocal("instance");
                if (IsNullable(prop))
                {
                    var propLocal = il.DeclareLocal(prop.PropertyType);
                    il.LoadLocalAddress(propLocal);
                    il.InitializeObject(prop.PropertyType);
                    il.LoadLocal(propLocal);
                }
                else 
                    il.LoadNull();
                    
                il.Call(prop.SetMethod);

                il.Branch($"prop{prop.Name}End");

                il.MarkLabel($"prop{prop.Name}IsNotNull");
            }
            
            if (type.IsValueType)
                il.LoadLocalAddress("instance");
            else
                il.LoadLocal("instance");
                
            il.LoadArgumentAddress(0);
            il.LoadLocal("current");
            il.LoadLocal("size");
            il.Call(typeof(ReadOnlyMemory<byte>).GetMethod(nameof(ReadOnlyMemory<byte>.Slice), new[] {typeof(int), typeof(int)}));

            DeserializeProperty(il, prop);
            if (IsNullable(prop))
            {
                il.NewObject(prop.PropertyType, Nullable.GetUnderlyingType(prop.PropertyType));
            }
                
            il.Call(prop.SetMethod);
                
            // current += size;
            il.LoadLocal("current");
            il.LoadLocal("size");
            il.Add();
            il.StoreLocal("current");

            il.Branch($"prop{prop.Name}End");

            il.MarkLabel($"prop{prop.Name}End");
        }

        il.LoadLocal("instance");
        if (type.IsValueType)
            il.Box(type);

        il.LoadLocal("current");
        il.NewObject(typeof(ValueTuple<object, int>).GetConstructors().First());
        il.Return();

        return il.CreateDelegate();
    }

    private static void DeserializeProperty(Emit<Func<ReadOnlyMemory<byte>,(object, int)>> il, PropertyInfo prop)
    {
        var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);

        if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(bool))
        {
            il.StoreLocal(MEMORY_LOCAL);
            il.LoadLocalAddress(MEMORY_LOCAL);
            il.Call(sReadOnlyMemorySpan);
            il.StoreLocal(SPAN_LOCAL);
            il.LoadLocalAddress(SPAN_LOCAL);
            il.LoadConstant(0);
            il.Call(sReadOnlySpanItem);
            il.LoadIndirect<byte>();
            if (type != typeof(byte))
                il.Convert(type);
            return;
        }
        
        if (type == typeof(Guid))
        {
            il.StoreLocal(MEMORY_LOCAL);
            il.LoadLocalAddress(MEMORY_LOCAL);
            il.Call(sReadOnlyMemorySpan);
            il.NewObject(typeof(Guid).GetConstructor(new[] {typeof(ReadOnlySpan<byte>)}));
            return;
        }
        
        if (type == typeof(string))
        {
            il.Call(typeof(Generator).GetMethod(nameof(StringReadBytes), BindingFlags.Static|BindingFlags.NonPublic, new[] {typeof(ReadOnlyMemory<byte>)}));
            il.Call(typeof(StringReadResult).GetProperty(nameof(StringReadResult.Result))?.GetGetMethod());
            return;
        }
        
        if (type == typeof(byte[]))
        {
            il.StoreLocal(MEMORY_LOCAL);
            il.LoadLocalAddress(MEMORY_LOCAL);
            il.Call(typeof(ReadOnlyMemory<byte>).GetMethod(nameof(ReadOnlyMemory<byte>.ToArray)));
            return;
        }
        
        if (type == typeof(decimal))
        {
            il.Call(typeof(Generator).GetMethod(nameof(DecimalReadBytes),new[] {typeof(ReadOnlyMemory<byte>)}));
            return;
        }

        if (type == typeof(DateTime))
        {
            il.StoreLocal(MEMORY_LOCAL);
            il.LoadLocalAddress(MEMORY_LOCAL);
            il.Call(sReadOnlyMemorySpan);
            il.Call(typeof(BitConverter).GetMethod(nameof(BitConverter.ToInt64), new[] {typeof(ReadOnlySpan<byte>)}));
            il.Call(typeof(DateTime).GetMethod(nameof(DateTime.FromBinary)));
            return;
        }
        
        if (Sil.IsSystemType(type) && type.IsValueType)
        {
            il.StoreLocal(MEMORY_LOCAL);
            il.LoadLocalAddress(MEMORY_LOCAL);
            il.Call(sReadOnlyMemorySpan);
            il.Call(
                typeof(BitConverter).GetMethods()
                    .First(x =>
                        x.Name == $"To{type.Name}" &&
                        x.GetParameters().Any(p => p.ParameterType == typeof(ReadOnlySpan<byte>))
                    )
            );
            return;
        }
        
        /*if (propertyType.IsArray)
        {
            var method = typeof(Sil).GetMethod(nameof(DeserializeArray),
                    BindingFlags.Static | BindingFlags.NonPublic)?
                .MakeGenericMethod(propertyType.GetElementType() ?? propertyType);
            if (method == null)
                throw new SilException("Unsupported array collection");

            il.Call(method);
        } */
        
        var dictionaryProp = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>) ? 
            type : type.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        if (dictionaryProp != null)
        {
            var typeArgs = dictionaryProp.GetGenericArguments()
                .Concat(new[] {type.IsInterface ? typeof(Dictionary<,>).MakeGenericType(dictionaryProp.GetGenericArguments()) : type}).ToArray();
            
            il.Call(typeof(Generator).GetMethod(nameof(DeserializeDictionary),
                    BindingFlags.Static | BindingFlags.NonPublic)?
                .MakeGenericMethod(typeArgs));

            return;
        }

        var enumerableProp = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ? 
            type : type.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumerableProp != null)
        {
            MethodInfo method;
            if(type.IsArray)
                method = typeof(Generator).GetMethod(nameof(DeserializeArray), BindingFlags.Static | BindingFlags.NonPublic);
            else
            {
                var genericType = type.GetGenericTypeDefinition();
                method = typeof(Generator).GetMethod(nameof(DeserializeEnumerable),
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (genericType == typeof(List<>))
                    method = typeof(Generator).GetMethod(nameof(DeserializeList),
                        BindingFlags.Static | BindingFlags.NonPublic);
                if (genericType == typeof(Stack<>))
                    method = typeof(Generator).GetMethod(nameof(DeserializeStack),
                        BindingFlags.Static | BindingFlags.NonPublic);
                if (genericType == typeof(Queue<>))
                    method = typeof(Generator).GetMethod(nameof(DeserializeQueue),
                        BindingFlags.Static | BindingFlags.NonPublic);
                if (genericType == typeof(HashSet<>))
                    method = typeof(Generator).GetMethod(nameof(DeserializeHashSet),
                        BindingFlags.Static | BindingFlags.NonPublic);
            }

            il.Call(method?.MakeGenericMethod(enumerableProp.GetGenericArguments()));
            
            return;
        }
        
        /*if (type.IsGenericType)
        {
            MethodInfo method;
            var genericType = type.GetGenericTypeDefinition();
            if (genericType == typeof(IEnumerable<>) || genericType == typeof(IList<>) || 
                genericType == typeof(ICollection<>) || genericType == typeof(ISet<>) ||
                genericType == typeof(IReadOnlyCollection<>) || genericType == typeof(IReadOnlyList<>))
            {
                method = typeof(Sil).GetMethod(nameof(DeserializeEnumerable),
                        BindingFlags.Static | BindingFlags.NonPublic)?
                    .MakeGenericMethod(propertyType.GetGenericArguments()[0]);
            }
            else if (genericType == typeof(List<>))
            {
                method = typeof(Sil).GetMethod(nameof(DeserializeList),
                        BindingFlags.Static | BindingFlags.NonPublic)?
                    .MakeGenericMethod(propertyType.GetGenericArguments()[0], propertyType);
            }
            else if (genericType == typeof(IReadOnlyDictionary<,>))
            {
                var args = propertyType.GetGenericArguments();
                method = typeof(Sil).GetMethod(nameof(DeserializeReadOnlyDictionary),
                        BindingFlags.Static | BindingFlags.NonPublic)?
                    .MakeGenericMethod(args[0], args[1]);
            }
            else if (genericType == typeof(Dictionary<,>) || genericType == typeof(IDictionary<,>) || genericType == typeof(IReadOnlyDictionary<,>))
            {
                var args = propertyType.GetGenericArguments();
                method = typeof(Sil).GetMethod(nameof(DeserializeDictionary),
                        BindingFlags.Static | BindingFlags.NonPublic)?
                    .MakeGenericMethod(args[0], args[1], propertyType);
            }
            else
                throw new SilException("Unsupported generic interface");
                
            method = typeof(Sil).GetMethod(nameof(DeserializeEnumerable),
                    BindingFlags.Static | BindingFlags.NonPublic)?
                .MakeGenericMethod(propertyType.GetGenericArguments()[0], propertyType);

            if (method == null)
                throw new SilException($"Unsupported collection");

            il.Call(method);
        }*/

        il.Call(typeof(Sil).GetMethod(nameof(Sil.SubDeserialize), BindingFlags.Static|BindingFlags.NonPublic));
        il.UnboxAny(type);
    }

    internal record StringReadResult(string Result, int Read);
    
    internal static unsafe StringReadResult StringReadBytes(ReadOnlyMemory<byte> source)
    {
        if (source.Length == 1 && source.Span[0] == 0)
            return new StringReadResult(string.Empty, 1);
        
        var destination = new char[source.Length / sizeof(char)];
        
        fixed (char* pointer = destination)
        {
            Buffer.MemoryCopy(source.Pin().Pointer, pointer, source.Length, source.Length);
        }

        return new StringReadResult(new string(destination), source.Length + 2);
    }

    internal static decimal DecimalReadBytes(ReadOnlyMemory<byte> source)
    {
        var span = source.Span;
        var bits = new[]
        {
            BitConverter.ToInt32(new []{span[0], span[1], span[2], span[3]}),
            BitConverter.ToInt32(new []{span[4], span[5], span[6], span[7]}),
            BitConverter.ToInt32(new []{span[8], span[9], span[10], span[11]}),
            BitConverter.ToInt32(new []{span[12], span[13], span[14], span[15]}),
        };
        return new decimal(bits);
    }
}