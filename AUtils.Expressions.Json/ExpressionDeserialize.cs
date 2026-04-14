using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using AUtils.IoC;

namespace AUtils.Expressions.Json;

internal static class ExpressionDeserialize
{
    public static Expression GetBlockExpression(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var body = DeserializeArguments(ref reader, options, nameof(BlockExpression.Expressions));
        var vars = DeserializeArguments(ref reader, options, nameof(BlockExpression.Variables))
            .OfType<ParameterExpression>()
            .ToArray();

        if (vars.Length == 0)
            return Expression.Block(body);

        var substitutor = new ParametersSubstitutor(vars.ToList());
        Expression bodyBlock = Expression.Block(vars, body);
        bodyBlock = substitutor.Visit(bodyBlock);
        
        return bodyBlock;
    }

    public static Expression GetRuntimeVariablesExpression(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (!reader.Read() || reader.GetString() != nameof(RuntimeVariablesExpression.Variables))
            throw new JsonException();

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        var currentReader = reader;
        var variables = new List<ParameterExpression>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            var item = (ParameterExpression?)JsonSerializer.Deserialize<Expression>(ref currentReader, options) ??
                       throw new JsonException();
            variables.Add(item);
            reader = currentReader;
        }

        return Expression.RuntimeVariables(variables);
    }

    public static Expression GetSwitchExpression(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (!reader.Read() || reader.GetString() != nameof(SwitchExpression.Cases))
            throw new JsonException();

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        var currentReader = reader;
        var cases = new List<SwitchCase>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (!currentReader.Read() || currentReader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            if (!currentReader.Read() || currentReader.GetString() != nameof(SwitchCase.Body))
                throw new JsonException();

            var body = JsonSerializer.Deserialize<Expression>(ref currentReader, options) ?? throw new JsonException();

            var args = DeserializeArguments(ref currentReader, options, nameof(SwitchCase.TestValues));

            if (!currentReader.Read() || currentReader.TokenType != JsonTokenType.EndObject)
                throw new JsonException();

            cases.Add(Expression.SwitchCase(body, args));
            reader = currentReader;
        }

        var comparison = DeserializeProperty<MethodInfo>(ref reader, options, nameof(SwitchExpression.Comparison));
        var defaultBody = DeserializeProperty<Expression>(ref reader, options, nameof(SwitchExpression.DefaultBody));
        var value = DeserializeProperty<Expression>(ref reader, options, nameof(SwitchExpression.SwitchValue));

        return Expression.Switch(value, defaultBody, comparison, cases);
    }

    public static Expression GetLoopExpression(ref Utf8JsonReader reader, JsonSerializerOptions options,
        Dictionary<string, LabelTarget> labelTargets)
    {
        var obj = DeserializeProperty<Expression>(ref reader, options, nameof(LoopExpression.Body));
        var continueLabel = GetLabelTarget(ref reader, options, nameof(LoopExpression.ContinueLabel), labelTargets);
        var breakLabel = GetLabelTarget(ref reader, options, nameof(LoopExpression.BreakLabel), labelTargets);

        return Expression.Loop(obj, breakLabel, continueLabel);
    }

    public static Expression GetNewArrayExpression(ref Utf8JsonReader reader, JsonSerializerOptions options,
        ExpressionType expressionType)
    {
        var type = DeserializeProperty<Type>(ref reader, options, nameof(NewArrayExpression.Type));
        var arguments = DeserializeArguments(ref reader, options, nameof(NewArrayExpression.Expressions));

        if (expressionType == ExpressionType.NewArrayInit)
            return Expression.NewArrayInit(type, arguments);
        if (expressionType == ExpressionType.NewArrayBounds)
            return Expression.NewArrayBounds(type, arguments);

        throw new JsonException();
    }

    public static Expression GetParameterExpression(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var name = ReadStringProperty(ref reader, nameof(ParameterExpression.Name));
        if (!reader.Read() || reader.GetString() != nameof(ParameterExpression.IsByRef))
            throw new JsonException();

        if (!reader.Read())
            throw new JsonException();

        var isByRef = reader.GetBoolean();
        var type = DeserializeProperty<Type>(ref reader, options, nameof(ParameterExpression.Type));

        return Expression.Parameter(isByRef ? type.MakeByRefType() : type, name);
    }

    public static Expression GetIndexerExpression(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var obj = DeserializeProperty<Expression>(ref reader, options, nameof(IndexExpression.Object));
        var property = DeserializeProperty<PropertyInfo>(ref reader, options, nameof(IndexExpression.Indexer));
        var arguments = DeserializeArguments(ref reader, options);

        return Expression.MakeIndex(obj, property, arguments);
    }

    public static Expression GetConstantExpression(ref Utf8JsonReader reader, JsonSerializerOptions options,
        IContainer container)
    {
        var type = DeserializeProperty<Type>(ref reader, options, nameof(ConstantExpression.Type));

        if (!reader.Read() || reader.GetString() != nameof(ConstantExpression.Value))
            throw new JsonException();
        var value = JsonSerializer.Deserialize(ref reader, type, options);

        if (type.IsValueType || value != null)
            return Expression.Constant(value, type);

        if (type == typeof(Random))
            return Expression.Constant(Random.Shared);

        //if (type.IsInterface)
        return Expression.Constant(container.Resolve(type));
    }

    public static Expression GetInvocationExpression(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var obj = DeserializeProperty<Expression>(ref reader, options, nameof(InvocationExpression.Expression));
        var arguments = DeserializeArguments(ref reader, options);

        return Expression.Invoke(obj, arguments);
    }

    public static Expression GetMethodCallExpression(ref Utf8JsonReader reader, JsonSerializerOptions options,
        ExpressionType expressionType)
    {
        var method = DeserializeProperty<MethodInfo>(ref reader, options, nameof(MethodCallExpression.Method));
        var obj = DeserializeProperty<Expression>(ref reader, options, nameof(MethodCallExpression.Object));
        var arguments = DeserializeArguments(ref reader, options);

        if (expressionType == ExpressionType.Call)
            return Expression.Call(obj, method, arguments);
        if (expressionType == ExpressionType.ArrayIndex)
            return Expression.ArrayIndex(obj, arguments);

        throw new JsonException();
    }

    public static Expression GetNewExpression(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var method = DeserializeProperty<ConstructorInfo>(ref reader, options, nameof(NewExpression.Constructor));
        var arguments = DeserializeArguments(ref reader, options);

        return Expression.New(method, arguments);
    }

    public static Expression GetMemberInitExpression(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var newExpression =
            (NewExpression)DeserializeProperty<Expression>(ref reader, options,
                nameof(MemberInitExpression.NewExpression));
        throw new NotImplementedException();

        //return Expression.MemberInit(newExpression);
    }

    public static Expression GetMemberAccessExpression(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var memberName = ReadStringProperty(ref reader, nameof(MemberExpression.Member));
        var memberContainsType = DeserializeProperty<Type>(ref reader, options, "Type");
        var body = DeserializeProperty<Expression>(ref reader, options, nameof(MemberExpression.Expression));

        var member = memberContainsType.GetMember(memberName)[0];
        return Expression.MakeMemberAccess(body, member);
    }

    public static Expression GetListInitExpression(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var newExpression =
            (NewExpression)DeserializeProperty<Expression>(ref reader, options,
                nameof(ListInitExpression.NewExpression));
        throw new NotImplementedException();

        //return Expression.ListInit(newExpression);
    }

    public static Expression GetConditionalExpression(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var test = DeserializeProperty<Expression>(ref reader, options, nameof(ConditionalExpression.Test));
        var ifTrue = DeserializeProperty<Expression>(ref reader, options, nameof(ConditionalExpression.IfTrue));
        var ifFalse = DeserializeProperty<Expression>(ref reader, options, nameof(ConditionalExpression.IfFalse));

        return Expression.Condition(test, ifTrue, ifFalse);
    }

    public static Expression GetBinaryExpression(ref Utf8JsonReader reader, JsonSerializerOptions options,
        ExpressionType type)
    {
        var left = DeserializeProperty<Expression>(ref reader, options, nameof(BinaryExpression.Left));
        var right = DeserializeProperty<Expression>(ref reader, options, nameof(BinaryExpression.Right));
        var conversion =
            DeserializeProperty<LambdaExpression>(ref reader, options, nameof(BinaryExpression.Conversion));
        var method = DeserializeProperty<MethodInfo>(ref reader, options, nameof(BinaryExpression.Method));

        if (!reader.Read() || reader.GetString() != nameof(BinaryExpression.IsLiftedToNull))
            throw new JsonException();

        if (!reader.Read())
            throw new JsonException();
        var isLiftedToNull = reader.GetBoolean();

        return Expression.MakeBinary(type, left, right, isLiftedToNull, method, conversion);
    }

    public static Expression GetDefaultExpression(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var type = DeserializeProperty<Type>(ref reader, options, nameof(UnaryExpression.Type));
        return Expression.Default(type);
    }

    public static Expression GetUnaryExpression(ref Utf8JsonReader reader, JsonSerializerOptions options,
        ExpressionType expressionType)
    {
        var value = DeserializeProperty<Expression>(ref reader, options, nameof(UnaryExpression.Operand));
        var method = DeserializeProperty<MethodInfo>(ref reader, options, nameof(UnaryExpression.Method));
        var type = DeserializeProperty<Type>(ref reader, options, nameof(UnaryExpression.Type));
        return Expression.MakeUnary(expressionType, value, type, method);
    }

    public static Expression GetTypeBinaryExpression(ref Utf8JsonReader reader, JsonSerializerOptions options,
        ExpressionType expressionType)
    {
        var value = DeserializeProperty<Expression>(ref reader, options, nameof(TypeBinaryExpression.Expression));
        var type = DeserializeProperty<Type>(ref reader, options, nameof(TypeBinaryExpression.TypeOperand));
        return expressionType switch
        {
            ExpressionType.TypeIs => Expression.TypeIs(value, type),
            ExpressionType.TypeEqual => Expression.TypeEqual(value, type),
            _ => throw new JsonException()
        };
    }

    public static Expression GetGotoExpression(ref Utf8JsonReader reader, JsonSerializerOptions options, Dictionary<string, LabelTarget> labelTargets)
    {
        var kind = ReadStringProperty(ref reader, nameof(GotoExpression.Kind));
        if (string.IsNullOrEmpty(kind) || !ExpressionConverter.LabelKind.ContainsValue(kind))
            throw new JsonException();
        var kindValue = ExpressionConverter.LabelKind.Single(x => x.Value == kind).Key;

        var label = GetLabelTarget(ref reader, options, nameof(GotoExpression.Target), labelTargets);
        var type = DeserializeProperty<Type>(ref reader, options, nameof(GotoExpression.Type));
        var value = DeserializeProperty<Expression>(ref reader, options, nameof(GotoExpression.Value));

        return Expression.MakeGoto(kindValue, label, value, type);
    }
    
    public static Expression GetTryExpression(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var body = DeserializeProperty<Expression>(ref reader, options, nameof(TryExpression.Body));
        var fault = DeserializeProperty<Expression>(ref reader, options, nameof(TryExpression.Fault));
        var @finally = DeserializeProperty<Expression>(ref reader, options, nameof(TryExpression.Finally));

        var catches = new List<CatchBlock>();
        if (!reader.Read() || reader.GetString() != nameof(TryExpression.Handlers))
            throw new JsonException();
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();
        while (reader.Read())
        {
            if(reader.TokenType == JsonTokenType.EndArray)
                break;
            
            var catchBody = DeserializeProperty<Expression>(ref reader, options, nameof(CatchBlock.Body));
            var variable = DeserializeProperty<Expression>(ref reader, options, nameof(CatchBlock.Variable));
            var filter = DeserializeProperty<Expression>(ref reader, options, nameof(CatchBlock.Filter));
            var type = DeserializeProperty<Type>(ref reader, options, nameof(CatchBlock.Test));
            catches.Add(Expression.MakeCatchBlock(type, (ParameterExpression) variable, catchBody, filter));
        }

        return Expression.MakeTry(null, body, @finally, fault, catches);
    }

    public static Expression GetLabelExpression(ref Utf8JsonReader reader, JsonSerializerOptions options,
        Dictionary<string, LabelTarget> labelTargets)
    {
        var label = GetLabelTarget(ref reader, options, nameof(LabelExpression.Target), labelTargets);
        return Expression.Label(label);
    }

    #region Utils
    
    private static LabelTarget GetLabelTarget(ref Utf8JsonReader reader, JsonSerializerOptions options, string name,
        Dictionary<string, LabelTarget> labelTargets)
    {
        var label = DeserializeProperty<LabelTarget>(ref reader, options, name);

        if (labelTargets.TryGetValue(label.Name, out var existedLabel))
            return existedLabel;

        labelTargets.Add(label.Name, label);
        return label;
    }

    private static Expression[] DeserializeArguments(ref Utf8JsonReader reader, JsonSerializerOptions options, string propName = "Arguments")
    {
        if (!reader.Read() || reader.GetString() != propName)
            throw new JsonException();

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        var expressions = new List<Expression>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            var item = JsonSerializer.Deserialize<Expression>(ref reader, options) ?? throw new JsonException();
            expressions.Add(item);
        }

        return expressions.ToArray();
    }

    private static T? DeserializeProperty<T>(ref Utf8JsonReader reader, JsonSerializerOptions options, string name)
    {
        if (!reader.Read() || reader.GetString() != name)
            throw new JsonException();
        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    public static string? ReadStringProperty(ref Utf8JsonReader reader, string propName)
    {
        if (!reader.Read() || reader.GetString() != propName)
            throw new JsonException();

        if (!reader.Read())
            throw new JsonException();

        return reader.GetString();
    }

    #endregion
}