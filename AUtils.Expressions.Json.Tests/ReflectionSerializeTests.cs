using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AUtils.Expressions.Json.Tests;

public class ReflectionSerializeTests
{
    [Fact]
    public void TypeSerialize()
    {
        var options = new JsonSerializerOptions
        {
            Converters = {new TypeConverter()}, 
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        var sourceJson = "{\"Name\":\"System.ValueTuple`2\",\"Arguments\":[{\"Name\":\"System.Collections.Generic.List`1\",\"Arguments\":[{\"Name\":\"System.Int32\"}]},{\"Name\":\"System.Collections.Generic.Dictionary`2\",\"Arguments\":[{\"Name\":\"System.ReadOnlyMemory`1\",\"Arguments\":[{\"Name\":\"System.Byte\"}]},{\"Name\":\"System.DateTime\"}]}]}";
        var sourceType = typeof(ValueTuple<List<int>, Dictionary<ReadOnlyMemory<byte>, DateTime>>);
        var json = JsonSerializer.Serialize(sourceType, options);
        Assert.Equal(sourceJson, json);
        var type = JsonSerializer.Deserialize<Type>(json, options);
        Assert.Equal(sourceType, type);
    }

    [Fact]
    public void PropertyInfoSerialize()
    {
        var options = new JsonSerializerOptions
        {
            Converters = {new TypeConverter(), new PropertyInfoConverter()}, 
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };
        
        var sourceJson = "{\"PropertyType\":{\"Name\":\"System.Collections.Generic.IEqualityComparer`1\",\"Arguments\":[{\"Name\":\"System.ReadOnlyMemory`1\",\"Arguments\":[{\"Name\":\"System.Byte\"}]}]},\"DeclaringType\":{\"Name\":\"System.Collections.Generic.Dictionary`2\",\"Arguments\":[{\"Name\":\"System.ReadOnlyMemory`1\",\"Arguments\":[{\"Name\":\"System.Byte\"}]},{\"Name\":\"System.DateTime\"}]},\"Name\":\"Comparer\"}";
        var sourceProperty = typeof(Dictionary<ReadOnlyMemory<byte>, DateTime>)
            .GetProperties()[0];
        var json = JsonSerializer.Serialize(sourceProperty, options);
        Assert.Equal(sourceJson, json);
        var propertyInfo = JsonSerializer.Deserialize<PropertyInfo>(json, options);
        Assert.Equal(sourceProperty, propertyInfo);
    }
    
    [Fact]
    public void MethodInfoSerialize()
    {
        var options = new JsonSerializerOptions
        {
            Converters = {new TypeConverter(), new MethodInfoConverter()}, 
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };
        
        var sourceJson = "{\"Name\":\"get_Comparer\",\"DeclaringType\":{\"Name\":\"System.Collections.Generic.Dictionary`2\",\"Arguments\":[{\"Name\":\"System.ReadOnlyMemory`1\",\"Arguments\":[{\"Name\":\"System.Byte\"}]},{\"Name\":\"System.DateTime\"}]},\"ReturnType\":{\"Name\":\"System.Collections.Generic.IEqualityComparer`1\",\"Arguments\":[{\"Name\":\"System.ReadOnlyMemory`1\",\"Arguments\":[{\"Name\":\"System.Byte\"}]}]},\"Parameters\":{}}";
        var sourceMethod = typeof(Dictionary<ReadOnlyMemory<byte>, DateTime>)
            .GetMethods()[0];
        var json = JsonSerializer.Serialize(sourceMethod, options);
        Assert.Equal(sourceJson, json);
        var methodInfo = JsonSerializer.Deserialize<MethodInfo>(json, options);
        Assert.Equal(sourceMethod, methodInfo);
    }
    
    [Fact]
    public void ConstructorInfoSerialize()
    {
        var options = new JsonSerializerOptions
        {
            Converters = {new TypeConverter(), new ConstructorInfoConverter()}, 
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };
        
        var sourceJson = "{\"DeclaringType\":{\"Name\":\"System.Collections.Generic.Dictionary`2\",\"Arguments\":[{\"Name\":\"System.ReadOnlyMemory`1\",\"Arguments\":[{\"Name\":\"System.Byte\"}]},{\"Name\":\"System.DateTime\"}]},\"Parameters\":[{\"Name\":\"System.Int32\"}]}";
        var sourceConstructor = typeof(Dictionary<ReadOnlyMemory<byte>, DateTime>)
            .GetConstructors().FirstOrDefault(x => x.GetParameters().Length > 0);
        var json = JsonSerializer.Serialize(sourceConstructor, options);
        Assert.Equal(sourceJson, json);
        var constructorInfo = JsonSerializer.Deserialize<ConstructorInfo>(json, options);
        Assert.Equal(sourceConstructor, constructorInfo);
    }
}