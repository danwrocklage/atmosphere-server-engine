using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AUtils.Expressions.Json;

public class LabelTargetConverter : JsonConverter<LabelTarget>
{
    public override LabelTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.Read() || reader.GetString() != nameof(LabelTarget.Type))
            throw new JsonException();

        var type = JsonSerializer.Deserialize<Type>(ref reader, options) ?? throw new JsonException();
        
        if (!reader.Read() || reader.GetString() != nameof(LabelTarget.Name))
            throw new JsonException();

        if (!reader.Read())
            throw new JsonException();
        var name = reader.GetString() ?? throw new JsonException();
        
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException();

        return Expression.Label(type, name);
    }

    public override void Write(Utf8JsonWriter writer, LabelTarget value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(JsonEncodedText.Encode(nameof(LabelTarget.Type)));
        JsonSerializer.Serialize(writer, value.Type, options);
        writer.WriteString(JsonEncodedText.Encode(nameof(LabelTarget.Name)), value.Name);
        writer.WriteEndObject();
    }
}