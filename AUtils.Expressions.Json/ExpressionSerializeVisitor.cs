using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace AUtils.Expressions.Json;

internal class ExpressionSerializeVisitor : ExpressionVisitor
{
    private readonly Utf8JsonWriter mJsonWriter;
    private readonly JsonSerializerOptions mJsonSerializerOptions;

    public ExpressionSerializeVisitor(Utf8JsonWriter jsonWriter, JsonSerializerOptions jsonSerializerOptions)
    {
        mJsonWriter = jsonWriter;
        mJsonSerializerOptions = jsonSerializerOptions;
    }

    public override Expression? Visit(Expression? node)
    {
        if(node == null)
            return base.Visit(node);
            
        mJsonWriter.WriteStartObject();
        WriteString(nameof(Expression.NodeType), ExpressionConverter.ExpressionTypes[node.NodeType]);
        var result = base.Visit(node);
        mJsonWriter.WriteEndObject();
        return result;
    }

    protected override Expression VisitBlock(BlockExpression node)
    {
        WriteArguments(node.Expressions, nameof(BlockExpression.Expressions));
        WriteArguments(new ReadOnlyCollection<Expression>(node.Variables.ToList<Expression>()), nameof(BlockExpression.Variables));
        return null;
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        WriteString(nameof(MemberAssignment.Member), node.Member.Name);
        WriteProp("Type");
        JsonSerializer.Serialize(mJsonWriter, node.Member.DeclaringType, mJsonSerializerOptions);
        WriteString(nameof(MemberAssignment.BindingType), node.BindingType.ToString());
        WriteProp(nameof(MemberAssignment.Expression));
        Visit(node.Expression);
            
        return null;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        WriteString(nameof(MemberAssignment.Member), node.Member.Name);
        WriteProp("Type");
        JsonSerializer.Serialize(mJsonWriter, node.Member.DeclaringType, mJsonSerializerOptions);
        WriteProp(nameof(MemberAssignment.Expression));
        Visit(node.Expression);
            
        return null;
    }

    protected override Expression VisitSwitch(SwitchExpression node)
    {
        WriteProp(nameof(SwitchExpression.Cases));
        mJsonWriter.WriteStartArray();
        foreach (var switchCase in node.Cases)
        {
            mJsonWriter.WriteStartObject();
            
            WriteProp(nameof(SwitchCase.Body));
            Visit(switchCase.Body);
            
            WriteArguments(switchCase.TestValues, nameof(SwitchCase.TestValues));
            mJsonWriter.WriteEndObject();
        }
        mJsonWriter.WriteEndArray();
        WriteProp(nameof(SwitchExpression.Comparison));
        JsonSerializer.Serialize(mJsonWriter, node.Comparison, mJsonSerializerOptions);
        WriteProp(nameof(SwitchExpression.DefaultBody));
        Visit(node.DefaultBody);
        WriteProp(nameof(SwitchExpression.SwitchValue));
        Visit(node.SwitchValue);
            
        return null;
    }

    protected override Expression VisitListInit(ListInitExpression node)
    {
        WriteProp(nameof(MemberInitExpression.NewExpression));
        Visit(node.NewExpression);
        WriteProp(nameof(ListInitExpression.Initializers));
        mJsonWriter.WriteStartArray();
        foreach (var elementInit in node.Initializers)
            VisitElementInit(elementInit);
        mJsonWriter.WriteEndArray();
            
        return null;
    }

    protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
    {
        VisitMemberBinding(node);
        WriteProp(nameof(MemberListBinding.Initializers));
        mJsonWriter.WriteStartArray();
        foreach (var elementInit in node.Initializers)
            VisitElementInit(elementInit);
        mJsonWriter.WriteEndArray();
        return null;
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        WriteProp(nameof(MemberInitExpression.NewExpression));
        Visit(node.NewExpression);
        WriteProp(nameof(MemberInitExpression.Bindings));
        mJsonWriter.WriteStartArray();
        foreach (var memberBinding in node.Bindings)
            VisitMemberBinding(memberBinding);
        mJsonWriter.WriteEndArray();
        return base.VisitMemberInit(node);
    }

    protected override MemberBinding VisitMemberBinding(MemberBinding node)
    {
        WriteString(nameof(MemberAssignment.Member.MemberType), node.Member.MemberType.ToString());
        WriteProp(nameof(MemberAssignment.Member));
        switch (node.Member)
        {
            case { MemberType: MemberTypes.Method }:
                JsonSerializer.Serialize(mJsonWriter, (MethodInfo) node.Member, mJsonSerializerOptions);
                break;
            case { MemberType: MemberTypes.Property }:
                JsonSerializer.Serialize(mJsonWriter, (PropertyInfo) node.Member, mJsonSerializerOptions);
                break;
            case { MemberType: MemberTypes.Constructor }:
                JsonSerializer.Serialize(mJsonWriter, (ConstructorInfo)node.Member, mJsonSerializerOptions);
                break;
            default:
                throw new NotSupportedException();
        }
                
        WriteString(nameof(MemberAssignment.BindingType), node.BindingType.ToString());
            
        return null;
    }
        
    protected override ElementInit VisitElementInit(ElementInit node)
    {
        WriteProp(nameof(ElementInit.AddMethod));
        JsonSerializer.Serialize(mJsonWriter, node.AddMethod, mJsonSerializerOptions);
        WriteArguments(node.Arguments);
        return null;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        WriteString(nameof(ParameterExpression.Name), node.Name);
        WriteProp(nameof(ParameterExpression.IsByRef));
        mJsonWriter.WriteBooleanValue(node.IsByRef);        
        WriteProp(nameof(ParameterExpression.Type));
        JsonSerializer.Serialize(mJsonWriter, node.Type, mJsonSerializerOptions);
            
        return null;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        WriteProp(nameof(UnaryExpression.Operand));
        Visit(node.Operand);
        WriteProp(nameof(UnaryExpression.Method));
        JsonSerializer.Serialize(mJsonWriter, node.Method, mJsonSerializerOptions);
        WriteProp(nameof(UnaryExpression.Type));
        JsonSerializer.Serialize(mJsonWriter, node.Type, mJsonSerializerOptions);

        return null;
    }

    protected override Expression VisitNew(NewExpression node)
    {
        WriteProp(nameof(NewExpression.Constructor));
        JsonSerializer.Serialize(mJsonWriter, node.Constructor, mJsonSerializerOptions);
        WriteArguments(node.Arguments);
            
        return null;
    }

    protected override Expression VisitLabel(LabelExpression node)
    {
        if (string.IsNullOrEmpty(node.Target.Name))
            throw new JsonException();
            
        WriteProp(nameof(LabelExpression.Target));
        JsonSerializer.Serialize(mJsonWriter, node.Target, mJsonSerializerOptions);
        return null;
    }

    protected override Expression VisitInvocation(InvocationExpression node)
    {
        WriteProp(nameof(InvocationExpression.Expression));
        Visit(node.Expression);

        WriteArguments(node.Arguments);
        return null;
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        WriteProp(nameof(IndexExpression.Indexer));
        JsonSerializer.Serialize(mJsonWriter, node.Indexer, mJsonSerializerOptions);
        WriteProp(nameof(IndexExpression.Object));
        if(node.Object == null)
            mJsonWriter.WriteNullValue();
        else
            Visit(node.Object);

        WriteArguments(node.Arguments);
        return null;
    }

    protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
    {
        WriteProp(nameof(RuntimeVariablesExpression.Variables));
        mJsonWriter.WriteStartArray();
        foreach (var nodeArgument in node.Variables)
            Visit(nodeArgument);
        mJsonWriter.WriteEndArray();

        return null;
    }

    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        WriteProp(nameof(NewArrayExpression.Type));
        JsonSerializer.Serialize(mJsonWriter, node.Type, mJsonSerializerOptions);
        WriteArguments(node.Expressions, nameof(NewArrayExpression.Expressions));

        return null;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        WriteProp(nameof(LambdaExpression));
        JsonSerializer.Serialize(mJsonWriter, (LambdaExpression)node, mJsonSerializerOptions);
            
        return null;
    }

    protected override Expression VisitDefault(DefaultExpression node)
    {
        WriteProp(nameof(DefaultExpression.Type));
        JsonSerializer.Serialize(mJsonWriter, node.Type, mJsonSerializerOptions);
            
        return null;
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        WriteProp(nameof(ConditionalExpression.Test));
        Visit(node.Test);
        WriteProp(nameof(ConditionalExpression.IfTrue));
        Visit(node.IfTrue);
        WriteProp(nameof(ConditionalExpression.IfFalse));
        Visit(node.IfFalse);
        return null;
    }

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        WriteProp(nameof(TypeBinaryExpression.Expression));
        Visit(node.Expression);
        WriteProp(nameof(TypeBinaryExpression.TypeOperand));
        JsonSerializer.Serialize(mJsonWriter, node.TypeOperand, mJsonSerializerOptions);
        return null;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        WriteProp(nameof(BinaryExpression.Left));
        Visit(node.Left);
        WriteProp(nameof(BinaryExpression.Right));
        Visit(node.Right);
        WriteProp(nameof(BinaryExpression.Conversion));
        JsonSerializer.Serialize(mJsonWriter, node.Conversion, mJsonSerializerOptions);
        WriteProp(nameof(BinaryExpression.Method));
        JsonSerializer.Serialize(mJsonWriter, node.Method, mJsonSerializerOptions);
        WriteProp(nameof(BinaryExpression.IsLiftedToNull));
        JsonSerializer.Serialize(mJsonWriter, node.IsLiftedToNull, mJsonSerializerOptions);
        return null;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        WriteProp(nameof(MethodCallExpression.Method));
        JsonSerializer.Serialize(mJsonWriter, node.Method, mJsonSerializerOptions);
            
        WriteProp(nameof(MethodCallExpression.Object));
        if(node.Object == null)
            mJsonWriter.WriteNullValue();
        else
            Visit(node.Object);
            
        WriteArguments(node.Arguments);
        return null;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        WriteProp(nameof(ConstantExpression.Type));
        JsonSerializer.Serialize(mJsonWriter, node.Type, mJsonSerializerOptions);
        WriteProp(nameof(ConstantExpression.Value));
        WriteConstantValue(node.Type.IsClass && node.Type.IsAbstract ? node.Value?.GetType() ?? node.Type : node.Type, node.Value);
        return null;
    }

    private void WriteConstantValue(Type nodeType, object? value)
    {
        if (nodeType.IsAssignableTo(typeof(Exception)))
            throw new NotSupportedException();
            
        if (nodeType.IsValueType)
            JsonSerializer.Serialize(mJsonWriter, value, mJsonSerializerOptions);
        else if (value == null)
            mJsonWriter.WriteNullValue();
        else if (nodeType == typeof(string))
            mJsonWriter.WriteStringValue(value as string);
        else
            mJsonWriter.WriteNullValue();
    }
        
    protected override Expression VisitLoop(LoopExpression node)
    {
        WriteProp(nameof(LoopExpression.Body));
        Visit(node.Body);

        WriteProp(nameof(LoopExpression.ContinueLabel));
        JsonSerializer.Serialize(mJsonWriter, node.ContinueLabel, mJsonSerializerOptions);
        WriteProp(nameof(LoopExpression.BreakLabel));
        JsonSerializer.Serialize(mJsonWriter, node.BreakLabel, mJsonSerializerOptions);
            
        return null;
    }

    protected override Expression VisitGoto(GotoExpression node)
    {
        WriteString(nameof(GotoExpression.Kind),  ExpressionConverter.LabelKind[node.Kind]);
        WriteProp(nameof(GotoExpression.Target));
        JsonSerializer.Serialize(mJsonWriter, node.Target, mJsonSerializerOptions);
        WriteProp(nameof(GotoExpression.Type));
        JsonSerializer.Serialize(mJsonWriter, node.Type, mJsonSerializerOptions);
        WriteProp(nameof(GotoExpression.Value));
        JsonSerializer.Serialize(mJsonWriter, node.Value, mJsonSerializerOptions);
            
        return null;
    }
    
    protected override Expression VisitTry(TryExpression node)
    {
        WriteProp(nameof(TryExpression.Body));
        JsonSerializer.Serialize(mJsonWriter, node.Body, mJsonSerializerOptions);
        WriteProp(nameof(TryExpression.Fault));
        JsonSerializer.Serialize(mJsonWriter, node.Fault, mJsonSerializerOptions);
        WriteProp(nameof(TryExpression.Finally));
        JsonSerializer.Serialize(mJsonWriter, node.Finally, mJsonSerializerOptions);
        
        WriteProp(nameof(TryExpression.Handlers));
        mJsonWriter.WriteStartArray();
        foreach (var catchBlock in node.Handlers)
        {
            WriteProp(nameof(CatchBlock.Body));
            JsonSerializer.Serialize(mJsonWriter, catchBlock.Body, mJsonSerializerOptions);
            WriteProp(nameof(CatchBlock.Variable));
            JsonSerializer.Serialize<Expression?>(mJsonWriter, catchBlock.Variable, mJsonSerializerOptions);
            WriteProp(nameof(CatchBlock.Filter));
            JsonSerializer.Serialize(mJsonWriter, catchBlock.Filter, mJsonSerializerOptions);
            WriteProp(nameof(CatchBlock.Test));
            JsonSerializer.Serialize(mJsonWriter, catchBlock.Test, mJsonSerializerOptions);
        }
        mJsonWriter.WriteEndArray();
        return null;
    }

    protected override CatchBlock VisitCatchBlock(CatchBlock node) => 
        throw new NotSupportedException();

    #region Not Supported

    protected override Expression VisitDynamic(DynamicExpression node) => throw new NotSupportedException();
    protected override Expression VisitDebugInfo(DebugInfoExpression node) => throw new NotSupportedException();
    protected override Expression VisitExtension(Expression node) => throw new NotSupportedException();

    #endregion

    #region Utils methods

    private void WriteString(string name, string? value) => mJsonWriter.WriteString(JsonEncodedText.Encode(name), value);

    private void WriteProp(string name) => mJsonWriter.WritePropertyName(JsonEncodedText.Encode(name));

    private void WriteArguments(ReadOnlyCollection<Expression> arguments, string propName = "Arguments")
    {
        WriteProp(propName);
        mJsonWriter.WriteStartArray();
        foreach (var nodeArgument in arguments)
            Visit(nodeArgument);
        mJsonWriter.WriteEndArray();
    }

    #endregion
}