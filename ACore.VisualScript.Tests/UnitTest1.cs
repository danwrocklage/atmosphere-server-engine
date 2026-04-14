using System.Linq.Expressions;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ACore.Tests.Shared;
using AUtils.Expressions.Async;
using AUtils.Expressions.Json;
using AUtils.IoC;
using LambdaExpression = System.Linq.Expressions.LambdaExpression;

namespace ACore.VisualScript.Tests;

public class UnitTest1
{
    private IContainer BuildContainer()
    {
        var builder = new ContainerBuilder();
        builder.AddFakeServices();
        new VisualScriptModule().ConfigureServices(builder);
        return builder.Build();
    }
    
    [Fact]
    public void Test1()
    {
        var container = BuildContainer();
        var nodes = container.Resolve<IScriptNodeService>();
        var process = container.Resolve<IScriptProcessService>();
        var script = container.Resolve<IScriptService>();
    }

    [Fact]
    public void LambdaExpressionSerializationTest()
    {
        var container = BuildContainer();
        var jsonOptions = new JsonSerializerOptions()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
            Converters =
            {
                new LambdaExpressionConverter(),
                new ExpressionConverter(container),
                new MethodInfoConverter(),
                new PropertyInfoConverter(),
                new ConstructorInfoConverter(),
                new LabelTargetConverter(),
                new TypeConverter()
            }
        };
        var l = (Expression<Func<int, int>>) (x => x + 3);
        var json = JsonSerializer.Serialize<LambdaExpression>(l, jsonOptions);
        var newL = JsonSerializer.Deserialize<LambdaExpression>(json, jsonOptions);
        var result = newL.Compile().DynamicInvoke(5);
        Assert.Equal(8, result);
    }
    
    [Fact]
    public async Task AsyncLambdaTest()
    {
        var container = BuildContainer();
        var jsonOptions = new JsonSerializerOptions()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
            Converters =
            {
                new LambdaExpressionConverter(),
                new ExpressionConverter(container),
                new MethodInfoConverter(),
                new PropertyInfoConverter(),
                new ConstructorInfoConverter(),
                new LabelTargetConverter(),
                new TypeConverter()
            }
        };
        
        var asyncMethod =
            typeof(UnitTest1).GetMethod(nameof(TestAsyncMethod), BindingFlags.Static | BindingFlags.Public)!;
        
        var vars = new[] {Expression.Variable(typeof(string), "testValue")};
        var parameters = new[] {Expression.Parameter(typeof(string), "arg")};
        var body = Expression.Block(vars, new List<Expression>
        {
            Expression.Assign(vars[0], Expression.Constant("SomeValue")),
            Expression.Assign(vars[0], new AwaitExpression(Expression.Call(null, asyncMethod, parameters[0]))),
            new AsyncResultExpression(vars[0], false)
        });

        var ol = AsyncExpression.Lambda(body, parameters, typeof(Task<string>), false, false);
        var method = (Func<string, Task<string>>) ol?.Compile()!;
        var result = await method("aaa+");
        Assert.Equal("aaa+_async tail", result);

        var json = JsonSerializer.Serialize<LambdaExpression>(ol, jsonOptions);
        var newL = JsonSerializer.Deserialize<LambdaExpression>(json, jsonOptions);
        method = (Func<string, Task<string>>) newL?.Compile()!;
        result = await method("bbb+");
        Assert.Equal("bbb+_async tail", result);
    }

    public static async Task<string> TestAsyncMethod(string value)
    {
        await Task.Delay(100);
        return value + "_async tail";
    }
}