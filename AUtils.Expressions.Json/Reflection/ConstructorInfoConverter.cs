using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AUtils.Expressions.Json;

public class ConstructorInfoConverter : JsonConverter<ConstructorInfo>
{
    public override ConstructorInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.Read() || reader.GetString() != nameof(MethodInfo.DeclaringType))
            throw new JsonException();

        var declaringType = JsonSerializer.Deserialize<Type>(ref reader, options) ?? throw new JsonException();
            
        if (!reader.Read() || reader.GetString() != "Parameters")
            throw new JsonException();
            
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        var parameters = new List<Type>();
        while (reader.Read())
        {
            if(reader.TokenType == JsonTokenType.EndArray)
                break;
            var argType = JsonSerializer.Deserialize<Type>(ref reader, options) ?? throw new JsonException();
            parameters.Add(argType);
        }
        
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException();
    
        return declaringType.GetConstructor(parameters.ToArray()) ?? throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, ConstructorInfo value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(JsonEncodedText.Encode(nameof(ConstructorInfo.DeclaringType)));
        JsonSerializer.Serialize(writer, value.DeclaringType, options);
        writer.WritePropertyName(JsonEncodedText.Encode("Parameters"));
        writer.WriteStartArray();
        var args = value.GetParameters();
        foreach (var arg in args)
            JsonSerializer.Serialize(writer, arg.ParameterType, options);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}