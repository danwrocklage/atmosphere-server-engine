using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace ACore.MongoDb;

internal class BsonStructSerializerProvider : IBsonSerializationProvider
{
    private readonly Dictionary<Type, IBsonSerializer> mSerializers = new();

    public IBsonSerializer GetSerializer(Type type)
    {
        if (mSerializers.TryGetValue(type, out var serializer))
            return serializer;

        if (type.IsValueType && !type.IsEnum && !type.Namespace!.StartsWith("System"))
        {
            serializer = new BsonStructSerializer(type);
            mSerializers.Add(type, serializer);
            return serializer;
        }
        
        return null;
    }
}

internal class BsonStructSerializer : IBsonSerializer
{
    public BsonStructSerializer(Type valueType)
    {
        ValueType = valueType;
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        var nominalType = args.NominalType;
        var fields = nominalType.GetFields(BindingFlags.Instance | BindingFlags.Public);
        var propsAll = nominalType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        
        context.Writer.WriteStartDocument();

        foreach (var field in fields)
        {
            context.Writer.WriteName(field.Name);
            BsonSerializer.Serialize(context.Writer, field.FieldType, field.GetValue(value));
        }
        foreach (var prop in propsAll)
        {
            if(!prop.CanWrite)
                continue;
            
            context.Writer.WriteName(prop.Name);
            BsonSerializer.Serialize(context.Writer, prop.PropertyType, prop.GetValue(value, null));
        }

        context.Writer.WriteEndDocument();
    }

    public Type ValueType { get; }

    public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        //boxing is required for SetValue to work
        var obj = Activator.CreateInstance(ValueType);
        var actualType = args.NominalType;
        var bsonReader = context.Reader;

        bsonReader.ReadStartDocument();

        while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var name = bsonReader.ReadName();

            var field = actualType.GetField(name);
            if (field != null)
            {
                var value = BsonSerializer.Deserialize(bsonReader, field.FieldType);
                field.SetValue(obj, value);
            }

            var prop = actualType.GetProperty(name);
            if (prop != null)
            {
                var value = BsonSerializer.Deserialize(bsonReader, prop.PropertyType);
                prop.SetValue(obj, value, null);
            }
        }

        bsonReader.ReadEndDocument();

        return obj;
    }
}