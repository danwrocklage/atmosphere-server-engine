using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AUtils.Expressions.Json;

public class LambdaExpressionConverter : JsonConverter<LambdaExpression>
{
    public override LambdaExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.Read() || reader.GetString() != nameof(LambdaExpression.Name))
            throw new JsonException();

        if (!reader.Read())
            throw new JsonException();
        var name = reader.GetString();
            
        if (!reader.Read() || reader.GetString() != nameof(LambdaExpression.Type))
            throw new JsonException();

        var type = JsonSerializer.Deserialize<Type>(ref reader, options);
        if (type == null)
            throw new JsonException();
            
        if (!reader.Read() || reader.GetString() != nameof(LambdaExpression.Body))
            throw new JsonException();

        var body = JsonSerializer.Deserialize<Expression>(ref reader, options);
        if (body == null)
            throw new JsonException();
            
        if (!reader.Read() || reader.TokenType == JsonTokenType.EndObject)
            return Expression.Lambda(type, body, name, Array.Empty<ParameterExpression>());
            
        if (reader.GetString() != nameof(LambdaExpression.Parameters))
            throw new JsonException();

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();
            
        var parameters = new List<string>();
        while (reader.Read())
        {
            if(reader.TokenType == JsonTokenType.EndArray)
                break;

            var parameter = reader.GetString();
            if(string.IsNullOrEmpty(parameter))
                continue;
            
            parameters.Add(parameter);
        }
        
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException();

        var substitutor = new ParametersSubstitutor(parameters);
        substitutor.Visit(body);
        
        return Expression.Lambda(type, body, name, substitutor.Parameters);
    }

    public override void Write(Utf8JsonWriter writer, LambdaExpression value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(JsonEncodedText.Encode(nameof(LambdaExpression.Name)), value.Name);
        writer.WritePropertyName(JsonEncodedText.Encode(nameof(LambdaExpression.Type)));
        JsonSerializer.Serialize(writer, value.Type, options);

        writer.WritePropertyName(JsonEncodedText.Encode(nameof(LambdaExpression.Body)));
        JsonSerializer.Serialize(writer, value.Body, options);

        if (value.Parameters.Count > 0)
        {
            writer.WritePropertyName(JsonEncodedText.Encode(nameof(LambdaExpression.Parameters)));
            writer.WriteStartArray();
            foreach (var valueParameter in value.Parameters)
                writer.WriteStringValue(valueParameter.Name);
            writer.WriteEndArray();
        }
            
        writer.WriteEndObject();
    }
}