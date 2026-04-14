using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AUtils.Expressions.Json;

public class MethodInfoConverter : JsonConverter<MethodInfo>
{
    public override MethodInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.Read() || reader.GetString() != nameof(MethodInfo.Name))
            throw new JsonException();

        if (!reader.Read())
            throw new JsonException();
        var methodName = reader.GetString() ?? throw new JsonException();
            
        if (!reader.Read() || reader.GetString() != nameof(MethodInfo.DeclaringType))
            throw new JsonException();

        var declaringType = JsonSerializer.Deserialize<Type>(ref reader, options) ?? throw new JsonException();
            
        if (!reader.Read() || reader.GetString() != nameof(MethodInfo.ReturnType))
            throw new JsonException();

        var returnType = JsonSerializer.Deserialize<Type>(ref reader, options) ?? throw new JsonException();
            
        if (!reader.Read() || (reader.GetString() != "Parameters" && reader.GetString() != "TypeArgs"))
            throw new JsonException();

        var typeArgs = new List<Type>();
        if (reader.GetString() == "TypeArgs")
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();
            
            while (reader.Read())
            {
                if(reader.TokenType == JsonTokenType.EndArray)
                    break;

                typeArgs.Add(JsonSerializer.Deserialize<Type>(ref reader, options) ?? throw new JsonException());
            }
            
            if (!reader.Read() || reader.GetString() != "Parameters")
                throw new JsonException();
        }
            
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        var parameters = new Dictionary<string, Type>();
        while (reader.Read())
        {
            if(reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            var argName = reader.GetString() ?? throw new JsonException();
            var argType = JsonSerializer.Deserialize<Type>(ref reader, options) ?? throw new JsonException();
            parameters.Add(argName, argType);
        }

        var methods = declaringType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        MethodInfo method = null;
        foreach (var methodInfo in methods)
        {
            if(methodInfo.Name != methodName)
                continue;

            var resultMethodInfo = methodInfo;
            if(typeArgs.Count > 0)
            {
                if (methodInfo.GetGenericArguments().Length != typeArgs.Count)
                    continue;
                resultMethodInfo = methodInfo.MakeGenericMethod(typeArgs.ToArray());
            }

            var methodParams = resultMethodInfo.GetParameters();
            if(methodParams.Length != parameters.Count)
                continue;
            
            var isMatch = true;
            foreach (var methodParam in methodParams)
            {
                if (parameters.TryGetValue(methodParam.Name, out var type) &&
                    type == methodParam.ParameterType) continue;
                
                isMatch = false;
                break;
            }

            if (isMatch)
            {
                method = resultMethodInfo;
                break;
            }
        }
        
        if (method == null || 
            !string.Equals(method.ReturnType.Name, returnType.Name, StringComparison.InvariantCultureIgnoreCase) || 
            !string.Equals(method.ReturnType.Namespace, returnType.Namespace, StringComparison.InvariantCultureIgnoreCase))
            throw new JsonException();

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException();

        return method;
    }

    public override void Write(Utf8JsonWriter writer, MethodInfo value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(JsonEncodedText.Encode(nameof(MethodInfo.Name)), value.Name);
        writer.WritePropertyName(JsonEncodedText.Encode(nameof(MethodInfo.DeclaringType)));
        JsonSerializer.Serialize(writer, value.DeclaringType, options);
        writer.WritePropertyName(JsonEncodedText.Encode(nameof(MethodInfo.ReturnType)));
        JsonSerializer.Serialize(writer, value.ReturnType, options);
        
        if (value.IsGenericMethod)
        {
            writer.WritePropertyName(JsonEncodedText.Encode("TypeArgs"));
            writer.WriteStartArray();
            foreach (var type in value.GetGenericArguments())
                JsonSerializer.Serialize(writer, type, options);
            writer.WriteEndArray();
        }
        
        writer.WritePropertyName(JsonEncodedText.Encode("Parameters"));
        writer.WriteStartObject();
        var args = value.GetParameters();
        foreach (var arg in args)
        {
            writer.WritePropertyName(JsonEncodedText.Encode(arg.Name ?? "Unknown"));
            JsonSerializer.Serialize(writer, arg.ParameterType, options);
        }
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}