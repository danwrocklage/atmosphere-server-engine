using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace AGame.Actors.Persistence;

internal static class PropertySerializer
{
    private static readonly ConcurrentDictionary<Type, Delegate> sSerializers = new();
    private static readonly ConcurrentDictionary<Type, Delegate> sDeserializers = new();

    public static BsonDocument Serialize(object item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        var type = item.GetType();
        var serializer = sSerializers.GetOrAdd(type, GenerateWriter);
        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);
        writer.WriteStartDocument();
        serializer.DynamicInvoke(item, writer);
        writer.WriteEndDocument();
        return document;
    }

    public static void Deserialize(object item, BsonDocument properties)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (properties == null) throw new ArgumentNullException(nameof(properties));
        var type = item.GetType();
        var deserializer = sDeserializers.GetOrAdd(type, GenerateReader);
        deserializer.DynamicInvoke(item, properties);
    }

    private static Delegate GenerateReader(Type type)
    {
        var props = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(x => x.CanRead && x.CanWrite && x.GetCustomAttribute<PersistenceAttribute>() != null)
            .ToDictionary(x => x.Name, x => x);

        var fields = type
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(x => x.GetCustomAttribute<PersistenceAttribute>() != null)
            .ToDictionary(x => x.Name, x => x);

        var instance = Expression.Parameter(type, "actor");
        var document = Expression.Parameter(typeof(BsonDocument), "doc");
        var currentBsonValue = Expression.Variable(typeof(BsonValue), "current");

        var bsonValues = typeof(BsonValue)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.Name.StartsWith("As") && !x.Name.StartsWith("AsBson") &&
#pragma warning disable CS0618
                        x.Name != nameof(BsonValue.AsLocalTime) && x.Name != nameof(BsonValue.AsUniversalTime))
#pragma warning restore CS0618
            .ToDictionary(x => x.PropertyType, x => x);

        var tryGetValue = typeof(BsonDocument).GetMethod(nameof(BsonDocument.TryGetValue),
            BindingFlags.Instance | BindingFlags.Public) ?? throw new Exception();

        var internalDeserialize = typeof(PropertySerializer).GetMethod(nameof(InternalDeserialize),
            BindingFlags.Static | BindingFlags.NonPublic) ?? throw new Exception();

        var body = new List<Expression>();

        foreach (var prop in props)
        {
            Expression convert = bsonValues.TryGetValue(prop.Value.PropertyType, out var converterProp)
                ? Expression.Property(currentBsonValue, converterProp)
                : Expression.Call(internalDeserialize.MakeGenericMethod(prop.Value.PropertyType), currentBsonValue);

            body.Add(Expression
                .IfThen(Expression
                        .Call(document, tryGetValue, Expression.Constant(prop.Key), currentBsonValue),
                    Expression.Assign(Expression.Property(instance, prop.Value), convert)));
        }

        foreach (var field in fields)
        {
            Expression convert = bsonValues.TryGetValue(field.Value.FieldType, out var converterProp)
                ? Expression.Property(currentBsonValue, converterProp)
                : Expression.Call(internalDeserialize.MakeGenericMethod(field.Value.FieldType), currentBsonValue);

            body.Add(Expression
                .IfThen(Expression
                        .Call(document, tryGetValue, Expression.Constant(field.Key), currentBsonValue),
                    Expression.Assign(Expression.Field(instance, field.Value), convert)));
        }

        var returnLabel = Expression.Label("returnLabel");
        body.Add(Expression.Return(returnLabel));
        body.Add(Expression.Label(returnLabel));

        return Expression
            .Lambda(
                typeof(Action<,>).MakeGenericType(type, typeof(BsonDocument)),
                Expression.Block(new[] {currentBsonValue}, body),
                instance, document)
            .Compile();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TValue InternalDeserialize<TValue>(BsonValue document) => 
        BsonSerializer.Deserialize<TValue>(document.ToBsonDocument());

    private static Delegate GenerateWriter(Type type)
    {
        var props = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(x => x.CanRead && x.CanWrite && x.GetCustomAttribute<PersistenceAttribute>() != null)
            .ToDictionary(x => x.Name, x => x);

        var fields = type
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(x => x.GetCustomAttribute<PersistenceAttribute>() != null)
            .ToDictionary(x => x.Name, x => x);

        var createContextMethod =
            typeof(BsonSerializationContext).GetMethod(nameof(BsonSerializationContext.CreateRoot),
                BindingFlags.Static | BindingFlags.Public) ?? throw new Exception();
        var writeMemberMethod =
            typeof(PropertySerializer).GetMethod(nameof(WriteMember), BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetGenericMethodDefinition() ?? throw new Exception();

        var instance = Expression.Parameter(type, "actor");
        var writer = Expression.Parameter(typeof(BsonDocumentWriter), "writer");
        var context = Expression.Variable(typeof(BsonSerializationContext), "context");

        var body = new List<Expression>
        {
            Expression.Assign(context,
                Expression.Call(createContextMethod, writer,
                    Expression.Constant(null, typeof(Action<BsonSerializationContext.Builder>))))
        };

        foreach (var (name, field) in fields)
        {
            body.Add(Expression.Call(writeMemberMethod.MakeGenericMethod(field.FieldType), writer,
                Expression.Constant(name), context, Expression.Field(instance, field)));
        }

        foreach (var (name, prop) in props)
        {
            body.Add(Expression.Call(writeMemberMethod.MakeGenericMethod(prop.PropertyType), writer,
                Expression.Constant(name), context, Expression.Property(instance, prop)));
        }

        var returnLabel = Expression.Label("returnLabel");
        body.Add(Expression.Return(returnLabel));
        body.Add(Expression.Label(returnLabel));

        return Expression
            .Lambda(
                typeof(Action<,>).MakeGenericType(type, typeof(BsonDocumentWriter)),
                Expression.Block(new[] {context}, body),
                instance, writer)
            .Compile();
    }

    private static void WriteMember<T>(BsonDocumentWriter writer, string name, BsonSerializationContext context,
        T value)
    {
        var serializer = BsonSerializer.LookupSerializer<T>();
        var args = new BsonSerializationArgs { NominalType = serializer.ValueType };
        writer.WriteName(name);
        serializer.Serialize(context, args, value);
    }
}