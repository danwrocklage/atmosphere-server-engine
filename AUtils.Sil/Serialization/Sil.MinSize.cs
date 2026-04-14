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
    private static readonly PropertyInfo sStringLen = typeof(string).GetProperty(nameof(string.Length));
    
    private static readonly ConcurrentDictionary<Type, (int FixedSize, Func<object, int> DynamicSize)> sMinSerializationSize =
        new();

    public static ushort OutputSize<T>(T instance)
    {
        if (instance == null)
            return 4;
        
        var type = typeof(T);
        var targetType = type == typeof(object) || type.IsAbstract || type.IsInterface ? 
            instance.GetType() : type;
        
        var size = OutputSize(targetType, instance);
        if (size > ushort.MaxValue)
            throw new SilException("Object is too large for serialization");

        return (ushort) size;
    }

    private static int OutputSize(Type type, object instance)
    {
        if (sMinSerializationSize.TryGetValue(type, out var sizer))
            return (sizer.DynamicSize?.Invoke(instance) ?? default) + sizer.FixedSize;
        
        if (IsSystemType(type) || type.IsEnum)
        {
            var size = 4;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                var method = typeof(Sil)
                    .GetMethod(nameof(OutputSize), BindingFlags.Static | BindingFlags.Public) ?? throw new SilException();
                var keyValueTypeInstanceParam = Expression.Parameter(typeof(object), "x");
                var keyValueTypeInstance = Expression.Convert(keyValueTypeInstanceParam, type);
                var keyValueTypeLambda = Expression.Lambda<Func<object, int>>(Expression.Block(Array.Empty<ParameterExpression>(), new List<Expression>
                {
                    Expression.Convert(Expression.Add(
                        Expression.Call(method.MakeGenericMethod(type.GetGenericArguments().First()),
                            Expression.Property(keyValueTypeInstance, "Key")), 
                        Expression.Call(method.MakeGenericMethod(type.GetGenericArguments().Last()), 
                            Expression.Property(keyValueTypeInstance, "Value"))
                    ), typeof(int))
                }), keyValueTypeInstanceParam);
                sizer = (size + 2 + 2, keyValueTypeLambda.Compile());
                sMinSerializationSize.TryAdd(type, sizer);
                return sizer.DynamicSize(instance) + sizer.FixedSize;
            }
            
            switch (instance)
            {
                case string stringInstance:
                    size += (stringInstance.Length == 0 ? 1 : stringInstance.Length * sizeof(char)) + 2;
                    sMinSerializationSize.TryAdd(type, (6, static i => ((string) i).Length == 0 ? 1 : ((string) i).Length * sizeof(char)));
                    break;
                case byte[] bytesInstance:
                    size += bytesInstance.Length + 2;
                    sMinSerializationSize.TryAdd(type, (6, static i => ((byte[]) i).Length));
                    break;
                default:
                    size += GetValueTypeSize(Nullable.GetUnderlyingType(type) ?? type);
                    sMinSerializationSize.TryAdd(type, (size, null));
                    break;
            }
            return size;
        }
        
        IsRegisteredOrThrow(type);

        var dynamicProps = new List<Expression>();

        var instanceParam = Expression.Parameter(typeof(object), "x");
        var sizeParam = Expression.Variable(typeof(int), "size");
        var fixedSize = GetSizeOf(type, Expression.Convert(instanceParam, type), dynamicProps, sizeParam);
        if (dynamicProps.Count == 0)
        {
            sMinSerializationSize.TryAdd(type, (fixedSize, null));
            return fixedSize;
        }

        var returnLabel = Expression.Label(sizeParam.Type, "MainReturn");
        dynamicProps.Add(Expression.Return(returnLabel, sizeParam));
        dynamicProps.Add(Expression.Label(returnLabel, Expression.Default(sizeParam.Type)));
        var lambda = Expression.Lambda<Func<object, int>>(Expression.Block(new [] { sizeParam }, dynamicProps), instanceParam);
        sizer = (fixedSize, lambda.Compile());
        sMinSerializationSize.TryAdd(type, sizer);
        return  sizer.DynamicSize(instance) + sizer.FixedSize;
    }

    private static int GetSizeOf(Type type, Expression parent, List<Expression> dynamicProps,
        ParameterExpression sizeParam)
    {
        var size = 4;
        foreach (var property in Generator.GetSerializingProperties(type))
        {
            if (property.PropertyType == type)
                throw new SilException($"Recursion is not supported ({type.Name}.{property.Name})");

            if (property.PropertyType == typeof(object))
            {
                size += 2;
                var prop = Expression.Property(parent, property);
                var subCall = typeof(Sil).GetMethod(nameof(OutputSize), BindingFlags.NonPublic | BindingFlags.Static,
                    new[] {typeof(Type), typeof(object)}) ?? throw new Exception();
                var getTypeMethod = typeof(object).GetMethod(nameof(GetType)) ?? throw new Exception();
                var condition = Expression.Condition(
                    Expression.Equal(prop, Expression.Constant(null, property.PropertyType)),
                    Expression.Constant(0),
                    Expression.Call(subCall, Expression.Call(prop, getTypeMethod), Expression.Convert(prop, typeof(object)))
                );
                dynamicProps.Add(Expression.AddAssign(sizeParam, Expression.Convert(condition, typeof(int))));
                continue;
            }
            
            if (property.PropertyType == typeof(byte[]))
            {
                size += 2;
                var prop = Expression.Property(parent, property);
                var condition = Expression.Condition(
                    Expression.Equal(prop, Expression.Constant(null, property.PropertyType)),
                    Expression.Constant(0),
                    Expression.ArrayLength(prop)
                );
                dynamicProps.Add(Expression.AddAssign(sizeParam, Expression.Convert(condition, typeof(int))));
                continue;
            }
                
            if (property.PropertyType == typeof(string))
            {
                size += 2;
                var prop = Expression.Property(parent, property);
                    
                var emptyCondition = Expression.Condition(
                    Expression.Equal(prop, Expression.Constant(string.Empty, property.PropertyType)),
                    Expression.Constant(1),
                    Expression.Multiply(Expression.Property(prop, sStringLen), Expression.Constant(sizeof(char))));
                    
                var condition = Expression.Condition(
                    Expression.Equal(prop, Expression.Constant(null, property.PropertyType)),
                    Expression.Constant(0), emptyCondition);
                    
                dynamicProps.Add(Expression.AddAssign(sizeParam, Expression.Convert(condition, typeof(int))));
                continue;
            }

            var enumerableType = property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ? 
                property.PropertyType : property.PropertyType.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (enumerableType != null)
            {
                size += 2;
                var prop = Expression.Property(parent, property);

                var collectionValueType = enumerableType.GetGenericArguments().First();

                var collectionSizeMethod = collectionValueType.IsGenericType && collectionValueType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>) ? (
                        typeof(Sil)
                            .GetMethod(nameof(GetDictionarySize), BindingFlags.Static | BindingFlags.NonPublic)
                            ?.MakeGenericMethod(collectionValueType.GetGenericArguments()) ?? throw new Exception()) : (typeof(Sil)
                    .GetMethod(nameof(GetCollectionSize), BindingFlags.Static | BindingFlags.NonPublic)
                    ?.MakeGenericMethod(collectionValueType) ?? throw new Exception());

                var condition = Expression.Condition(
                    Expression.Equal(prop, Expression.Constant(null, property.PropertyType)),
                    Expression.Constant(0),
                    Expression.Call(collectionSizeMethod, prop)
                );
                dynamicProps.Add(Expression.AddAssign(sizeParam, Expression.Convert(condition, typeof(int))));
                continue;
            }
            
            if (IsSystemType(property.PropertyType) || property.PropertyType.IsEnum)
            {
                var nullable = Nullable.GetUnderlyingType(property.PropertyType);
                if (nullable != null)
                {
                    size += 2;
                    
                    int len;
                    if (IsSystemType(nullable) || nullable.IsEnum)
                        len = GetValueTypeSize(nullable);
                    else
                    {
                        IsRegisteredOrThrow(nullable);
                        len = GetSizeOf(nullable, Expression.Property(parent, property), dynamicProps, sizeParam);
                    }

                    var prop = Expression.Property(parent, property);
                    var condition = Expression.Condition(
                        Expression.Equal(prop, Expression.Constant(null, property.PropertyType)),
                        Expression.Constant(0),
                        Expression.Constant(len)
                    );
                    dynamicProps.Add(Expression.AddAssign(sizeParam, Expression.Convert(condition, typeof(int))));
                    continue;
                }
                
                size += GetValueTypeSize(property.PropertyType) + 2;
                continue;
            }

            IsRegisteredOrThrow(property.PropertyType);
            size += 2;
            
            var subDynamicProps = new List<Expression>();
            var propertyExpression = Expression.Property(parent, property);
            var subSize = GetSizeOf(property.PropertyType, propertyExpression, subDynamicProps, sizeParam);
            subDynamicProps.Insert(0, Expression.AddAssign(sizeParam, Expression.Constant(subSize, typeof(int))));
            dynamicProps.Add(property.PropertyType.IsValueType ? Expression.Block(subDynamicProps) : Expression.IfThen(
                Expression.NotEqual(propertyExpression, Expression.Constant(null, property.PropertyType)),
                Expression.Block(subDynamicProps)
            ));
        }

        return size;
    }

    private static int GetCollectionSize<T>(IEnumerable<T> items)
    {
        var size = 0;
        foreach (var item in items)
            size += OutputSize(item) + 2;
        return size;
    }
    
    private static int GetDictionarySize<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        var size = 0;
        foreach (var item in items)
            size += 
                OutputSize(item.Key) + 2 + 
                OutputSize(item.Value) + 2;
        return size;
    }

    internal static ushort GetValueTypeSize(Type type)
    {
        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);
        
        if (type == typeof(byte) ||
            type == typeof(bool) ||
            type == typeof(sbyte)) return 1;
        
        if (type == typeof(char) ||
            type == typeof(short) ||
            type == typeof(Half) ||
            type == typeof(ushort)) return 2;
        
        if (type == typeof(int) ||
            type == typeof(float) ||
            type == typeof(uint) ||
            type == typeof(DateOnly)) return 4;
        
        if (type == typeof(Index)) return 5;
        
        if (type == typeof(long) ||
            type == typeof(double) ||
            type == typeof(ulong) ||
            type == typeof(DateTime) ||
            type == typeof(TimeSpan) ||
            type == typeof(TimeOnly)) return 8;
        
        if (type == typeof(Range)) return 10;

        if (type == typeof(Guid) ||
            type == typeof(decimal)) return 16;

        throw new NotSupportedException($"Value type '{type.Name}' is not supported for serialization");
    }
}