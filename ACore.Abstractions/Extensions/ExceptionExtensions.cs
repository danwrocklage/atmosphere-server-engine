using System.Runtime.CompilerServices;
using System.Text;

namespace ACore.Abstractions.Extensions;

public static class ExceptionExtensions
{
    /// <summary>
    /// Concat exception message with inner exception messages (while exists)
    /// </summary>
    public static string GetFullMessage(this Exception exception)
    {
        if (exception == null) 
            throw new ArgumentNullException(nameof(exception));

        var builder = new StringBuilder();
        builder.Append(GetMessage(exception));
        
        if (exception is AggregateException aggregateException)
        {
            for (var i = 0; i < aggregateException.InnerExceptions.Count; i++)
            {
                var innerException = aggregateException.InnerExceptions[i];
                builder.AppendFormat("{1}[{0}] => {2}", i, Environment.NewLine, GetMessage(innerException));
            }

            return builder.ToString();
        }
        
        var inEx = exception.InnerException;
        while (inEx != null)
        {
            builder.AppendFormat("{0} => {1}", Environment.NewLine, GetMessage(inEx));
            inEx = inEx.InnerException;
        }

        return builder.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetMessage(Exception exception) => 
        $"[{exception.GetType().Name}] {exception.Message}";
}