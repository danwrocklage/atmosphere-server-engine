using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace AUtils.Expressions.Async;

internal static class Awaiter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TAwaiter>
    where TAwaiter : INotifyCompletion
{
    internal delegate bool IsCompletedGetter(ref TAwaiter awaiter);

    internal static readonly IsCompletedGetter IsCompleted;

    private static bool NotCompleted(ref TAwaiter awaiter) => throw new MissingMemberException(awaiter.GetType().FullName, nameof(IsCompleted));

    static Awaiter()
    {
        var awaiterType = typeof(TAwaiter);
        var isCompletedProperty = awaiterType.GetProperty(nameof(TaskAwaiter.IsCompleted), typeof(bool));
        if (isCompletedProperty is null)
        {
            IsCompleted = NotCompleted;
        }
        else if (awaiterType.IsValueType)
        {
            IsCompleted = isCompletedProperty.GetMethod?.CreateDelegate<IsCompletedGetter>() ?? NotCompleted;
        }
        else
        {
            var awaiterParam = Expression.Parameter(awaiterType.MakeByRefType());
            IsCompleted = Expression.Lambda<IsCompletedGetter>(Expression.Property(awaiterParam, isCompletedProperty), true, awaiterParam).Compile();
        }
    }
}