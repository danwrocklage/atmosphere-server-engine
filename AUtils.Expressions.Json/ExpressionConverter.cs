using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using AUtils.IoC;

namespace AUtils.Expressions.Json;

public class ExpressionConverter : JsonConverter<Expression>
{
    internal static readonly Dictionary<ExpressionType, string?> ExpressionTypes = Enum
        .GetValues<ExpressionType>()
        .ToDictionary(x => x, x => Enum.GetName(x));
        
    internal static readonly Dictionary<GotoExpressionKind, string?> LabelKind = Enum
        .GetValues<GotoExpressionKind>()
        .ToDictionary(x => x, x => Enum.GetName(x));

    private readonly IContainer mContainer;
    private readonly Dictionary<string, LabelTarget> mLabelTargets;

    public ExpressionConverter(IContainer container)
    {
        mContainer = container;
        mLabelTargets = new Dictionary<string, LabelTarget>();
    }

    public override Expression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var nodeTypeString = ExpressionDeserialize.ReadStringProperty(ref reader, nameof(Expression.NodeType));

        if (!ExpressionTypes.ContainsValue(nodeTypeString))
            throw new JsonException();

        var nodeType = ExpressionTypes.Single(x => x.Value == nodeTypeString).Key;

        Expression result;
        switch (nodeType)
        {
            case ExpressionType.AddAssign: 
            case ExpressionType.AndAssign: 
            case ExpressionType.Add: 
            case ExpressionType.AddChecked: 
            case ExpressionType.And: 
            case ExpressionType.AndAlso: 
            case ExpressionType.Coalesce: 
            case ExpressionType.Divide: 
            case ExpressionType.Equal: 
            case ExpressionType.ExclusiveOr: 
            case ExpressionType.GreaterThan: 
            case ExpressionType.GreaterThanOrEqual: 
            case ExpressionType.LeftShift: 
            case ExpressionType.LessThan: 
            case ExpressionType.LessThanOrEqual: 
            case ExpressionType.Modulo: 
            case ExpressionType.Multiply: 
            case ExpressionType.MultiplyChecked: 
            case ExpressionType.NotEqual: 
            case ExpressionType.Or: 
            case ExpressionType.OrElse: 
            case ExpressionType.Subtract: 
            case ExpressionType.Assign: 
            case ExpressionType.DivideAssign:
            case ExpressionType.ExclusiveOrAssign:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.ModuloAssign:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.OrAssign:
            case ExpressionType.PowerAssign:
            case ExpressionType.RightShiftAssign:
            case ExpressionType.SubtractAssign:
            case ExpressionType.AddAssignChecked:
            case ExpressionType.MultiplyAssignChecked:
            case ExpressionType.SubtractAssignChecked: 
            case ExpressionType.Power:
            case ExpressionType.TypeAs:
            case ExpressionType.RightShift:
            case ExpressionType.SubtractChecked:
                result = ExpressionDeserialize.GetBinaryExpression(ref reader, options, nodeType);
                break;

            case ExpressionType.ArrayLength: 
            case ExpressionType.Convert: 
            case ExpressionType.ConvertChecked: 
            case ExpressionType.Unbox: 
            case ExpressionType.Throw: 
            case ExpressionType.Negate: 
            case ExpressionType.UnaryPlus: 
            case ExpressionType.NegateChecked: 
            case ExpressionType.Not: 
            case ExpressionType.Quote: 
            case ExpressionType.Decrement:
            case ExpressionType.Increment: 
            case ExpressionType.PreIncrementAssign:
            case ExpressionType.PreDecrementAssign:
            case ExpressionType.PostIncrementAssign:
            case ExpressionType.PostDecrementAssign:
            case ExpressionType.OnesComplement:
            case ExpressionType.IsTrue:
            case ExpressionType.IsFalse:
                result = ExpressionDeserialize.GetUnaryExpression(ref reader, options, nodeType);
                break;

            case ExpressionType.TypeIs:
            case ExpressionType.TypeEqual:
                result = ExpressionDeserialize.GetTypeBinaryExpression(ref reader, options, nodeType);
                break;

            case ExpressionType.Goto: 
                result = ExpressionDeserialize.GetGotoExpression(ref reader, options, mLabelTargets);
                break;

            case ExpressionType.Label: 
                result = ExpressionDeserialize.GetLabelExpression(ref reader, options, mLabelTargets);
                break;

            case ExpressionType.Block:
                result = ExpressionDeserialize.GetBlockExpression(ref reader, options);
                break;

            case ExpressionType.Conditional:
                result = ExpressionDeserialize.GetConditionalExpression(ref reader, options);
                break;

            case ExpressionType.ArrayIndex:
            case ExpressionType.Call:
                result = ExpressionDeserialize.GetMethodCallExpression(ref reader, options, nodeType);
                break;
                
            case ExpressionType.Constant:
                result = ExpressionDeserialize.GetConstantExpression(ref reader, options, mContainer);
                break;
                
            case ExpressionType.Invoke:
                result = ExpressionDeserialize.GetInvocationExpression(ref reader, options);
                break;
                
            case ExpressionType.Lambda:
                if (!reader.Read() || reader.GetString() != nameof(LambdaExpression))
                    throw new JsonException();
                result = JsonSerializer.Deserialize<LambdaExpression>(ref reader, options) ?? throw new JsonException();
                break;
                
            case ExpressionType.Index:
                result = ExpressionDeserialize.GetIndexerExpression(ref reader, options);
                break;

            case ExpressionType.ListInit:
                result = ExpressionDeserialize.GetListInitExpression(ref reader, options);
                break;
                
            case ExpressionType.MemberAccess:
                result = ExpressionDeserialize.GetMemberAccessExpression(ref reader, options);
                break;
                
            case ExpressionType.MemberInit:
                result = ExpressionDeserialize.GetMemberInitExpression(ref reader, options);
                break;
                
            case ExpressionType.Switch:
                result = ExpressionDeserialize.GetSwitchExpression(ref reader, options);
                break;
                
            case ExpressionType.New:
                result = ExpressionDeserialize.GetNewExpression(ref reader, options);
                break;
                
            case ExpressionType.NewArrayInit:
            case ExpressionType.NewArrayBounds:
                result = ExpressionDeserialize.GetNewArrayExpression(ref reader, options, nodeType);
                break;
                
            case ExpressionType.Parameter:
                result = ExpressionDeserialize.GetParameterExpression(ref reader, options);
                break;
            case ExpressionType.RuntimeVariables:
                result = ExpressionDeserialize.GetRuntimeVariablesExpression(ref reader, options);
                break;
                
            case ExpressionType.Default:
                result = ExpressionDeserialize.GetDefaultExpression(ref reader, options);
                break;
                
            case ExpressionType.Loop:
                result = ExpressionDeserialize.GetLoopExpression(ref reader, options, mLabelTargets);
                break;
                
            case ExpressionType.Dynamic: throw new NotSupportedException();
            case ExpressionType.DebugInfo: throw new NotSupportedException();
            case ExpressionType.Extension: throw new NotSupportedException();
            case ExpressionType.Try: 
                result = ExpressionDeserialize.GetTryExpression(ref reader, options);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
            
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException();
            
        return result;
    }

    public override void Write(Utf8JsonWriter writer, Expression value, JsonSerializerOptions options)
    {
        var visitor = new ExpressionSerializeVisitor(writer, options);
        visitor.Visit(value);
    }
}