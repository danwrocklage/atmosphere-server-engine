using System.Linq.Expressions;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AUtils.Expressions.Json.Tests;

public class ExpressionSerializeTests
{
    [Fact]
    public void LabelTargetSerialize()
    {
        var options = new JsonSerializerOptions
        {
            Converters = {new TypeConverter(), new LabelTargetConverter()}, 
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };
        
        var sourceJson = "{\"Type\":{\"Name\":\"System.String\"},\"Name\":\"SomeNewLabel\"}";
        var sourceLabel = Expression.Label(typeof(string), "SomeNewLabel");
        var json = JsonSerializer.Serialize(sourceLabel, options);
        Assert.Equal(sourceJson, json);
        var labelTarget = JsonSerializer.Deserialize<LabelTarget>(json, options);
        Assert.NotNull(labelTarget);
        Assert.Equal(sourceLabel.Name, labelTarget.Name);
        Assert.Equal(sourceLabel.Type, labelTarget.Type);
    }
}