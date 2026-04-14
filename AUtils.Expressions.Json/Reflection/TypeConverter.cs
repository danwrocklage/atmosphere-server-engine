using System.Text.Json;
using System.Text.Json.Serialization;

namespace AUtils.Expressions.Json;

public class TypeConverter : JsonConverter<Type>
{
    private static IEnumerable<Type> All => AppDomain.CurrentDomain.GetAssemblies()
        .Where(x => x.FullName?.StartsWith("System") == false && x.FullName?.StartsWith("Microsoft") == false)
        .SelectMany(x => x.GetTypes())
        .Where(x => x.FullName?.StartsWith("System") == false && x.FullName?.StartsWith("Microsoft") == false);
    
    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.Read() || reader.GetString() != "Name")
            throw new JsonException();
        
        if (!reader.Read())
            throw new JsonException();
        var typeName = reader.GetString() ?? throw new JsonException();
        
        var type = typeName.StartsWith("System") ? Type.GetType(typeName) : All.FirstOrDefault(x =>
            string.Equals($"{x.Namespace}.{x.Name}", typeName, StringComparison.InvariantCultureIgnoreCase));

        if (!reader.Read() ||
            (reader.TokenType != JsonTokenType.PropertyName && reader.TokenType != JsonTokenType.EndObject))
            throw new JsonException();

        if (reader.TokenType == JsonTokenType.EndObject)
            return type;
        
        if (reader.GetString() != "Arguments")
            throw new JsonException();
        
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();
        
        var args = new List<Type>();
        while (reader.Read())
        {
            if(reader.TokenType == JsonTokenType.EndArray)
                break;
            var argType = JsonSerializer.Deserialize<Type>(ref reader, options) ?? throw new JsonException();
            args.Add(argType);
        }
        
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException();

        if (type == null)
            return null;

        return type.IsByRef ? 
            type.GetElementType()!.MakeGenericType(args.ToArray()).MakeByRefType() : 
            type.MakeGenericType(args.ToArray());
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        if (string.IsNullOrEmpty(value.Namespace) || string.IsNullOrEmpty(value.Name))
            throw new JsonException();
        
        writer.WriteStartObject();

        writer.WriteString("Name", $"{value.Namespace}.{value.Name}");
        if (value.IsByRef)
            value = value.GetElementType() ?? throw new Exception();
        if (value.IsGenericType)
        {
            writer.WritePropertyName("Arguments");
            writer.WriteStartArray();
            var args = value.GetGenericArguments();
            foreach (var type in args)
                JsonSerializer.Serialize(writer, type, options);
            
            writer.WriteEndArray();
        }
        
        writer.WriteEndObject();
    }
}