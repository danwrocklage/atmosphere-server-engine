using System.Text.Json;
using System.Text.Json.Serialization;

namespace ACore.Worker.Web;

/// <summary>
/// Store <see cref="Exception"/> as JSON string
/// </summary>
internal class ExceptionConverter : JsonConverter<Exception>
{
    public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(JsonEncodedText.Encode("Type"), value.GetType().FullName);
        writer.WriteString(JsonEncodedText.Encode(nameof(Exception.Message)), value.Message);
        writer.WriteString(JsonEncodedText.Encode(nameof(Exception.Source)), value.Source);
#if !Production
        writer.WriteString(JsonEncodedText.Encode(nameof(Exception.StackTrace)), value.StackTrace);
#endif
        writer.WritePropertyName(JsonEncodedText.Encode(nameof(Exception.Data)));
        JsonSerializer.Serialize(writer, value.Data, options);

        if (value is AggregateException aggregateException)
        {
            writer.WritePropertyName(JsonEncodedText.Encode(nameof(AggregateException.InnerExceptions)));
            writer.WriteStartArray();
            foreach (var innerException in aggregateException.InnerExceptions)
                JsonSerializer.Serialize(writer, innerException, options);
            writer.WriteEndArray();
        }
        else
        {
            writer.WritePropertyName(JsonEncodedText.Encode(nameof(Exception.InnerException)));
            JsonSerializer.Serialize(writer, value.InnerException, options);
        }
        writer.WriteEndObject();
    }
}