using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace AUtils.Expressions.Async;

[StructLayout(LayoutKind.Auto)]
internal readonly struct TaskType
{
    private readonly Type? mResultType;
    private readonly Type mTaskType;

    internal TaskType(Type? resultType, bool isValueTask)
    {
        mResultType = resultType;
        if (resultType is null || resultType == typeof(void))
            mTaskType = isValueTask ? typeof(ValueTask) : typeof(Task);
        else
            mTaskType = (isValueTask ? typeof(ValueTask<>) : typeof(Task<>)).MakeGenericType(resultType);
    }

    internal TaskType(Type taskType)
    {
        mTaskType = taskType;
        if (mTaskType == typeof(ValueTask) || mTaskType == typeof(Task))
            mResultType = null;
        else
        {
            var genericType = mTaskType.GetGenericTypeDefinition();
            if(genericType != typeof(ValueTask<>) && genericType != typeof(Task<>))
                throw new ArgumentException();

            mResultType = mTaskType.GetGenericArguments()[0];
        }
    }

    internal MethodCallExpression AdjustTaskType(MethodCallExpression startMachineCall)
        => IsValueTask ? startMachineCall : Expression.Call(startMachineCall, nameof(ValueTask.AsTask), Type.EmptyTypes);

    internal Type ResultType => mResultType ?? typeof(void);

    internal bool HasResult => mResultType is not null;

    internal bool IsValueTask => mTaskType is { IsValueType: true };

    public static implicit operator Type(in TaskType type) => type.mTaskType;
}