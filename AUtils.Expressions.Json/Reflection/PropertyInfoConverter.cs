using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AUtils.Expressions.Json;

public class PropertyInfoConverter : JsonConverter<PropertyInfo>
{
    public override PropertyInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.Read() || reader.GetString() != nameof(PropertyInfo.PropertyType))
            throw new JsonException();

        var propertyType = JsonSerializer.Deserialize<Type>(ref reader, options) ?? throw new JsonException();
            
        if (!reader.Read() || reader.GetString() != nameof(PropertyInfo.DeclaringType))
            throw new JsonException();

        var declaringType = JsonSerializer.Deserialize<Type>(ref reader, options) ?? throw new JsonException();
        
        if (!reader.Read() || reader.GetString() != nameof(PropertyInfo.Name))
            throw new JsonException();

        if (!reader.Read())
            throw new JsonException();
        var name = reader.GetString() ?? throw new JsonException();
        
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException();
  
        return declaringType.GetProperty(name, propertyType) ?? throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, PropertyInfo value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(JsonEncodedText.Encode(nameof(PropertyInfo.PropertyType)));
        JsonSerializer.Serialize(writer, value.PropertyType, options);
        writer.WritePropertyName(JsonEncodedText.Encode(nameof(PropertyInfo.DeclaringType)));
        JsonSerializer.Serialize(writer, value.DeclaringType, options);
        writer.WriteString(JsonEncodedText.Encode(nameof(PropertyInfo.Name)), value.Name);
        writer.WriteEndObject();
    }
}