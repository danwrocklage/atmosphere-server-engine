using System.Text;
using ACore.Abstractions;
using ACore.Abstractions.Extensions;
using ACore.Abstractions.Logging;

namespace ACore.Logging;

internal static class MessageFormatExtension
{
    private static readonly string sDotnetVersion = Environment.Version.ToString();
    private static readonly Dictionary<LogLevel, string> sLogLevels = Enum.GetValues<LogLevel>()
        .ToDictionary(x => x, x =>  Enum.GetName(x)?.PadLeft(8, ' ').ToUpperInvariant());

    internal static string Format(this Message message)
    {
        var builder = new StringBuilder();
        builder.Append(sLogLevels[message.Level]);
        builder.Append(' ');
        builder.Append(message.Time.ToString("hh:mm:ss.fff"));
        builder.Append(' ');
        if (message.ThreadId.HasValue)
        {
            builder.Append("thr-");            
            builder.Append(message.ThreadId.ToString().PadLeft(2, '0'));
        }
        else
            builder.Append("  --  ");
        builder.Append(' ');
        builder.Append(message.Section.ToUpperInvariant().PadLeft(15));
        builder.Append(' ');
        builder.Append(message.Text);
        return builder.ToString();
    }

    internal static LogstashEvent ToEvent(this in Message message, ICellEnvironment environment) =>
        new()
        {
            Level = sLogLevels[message.Level],
            Role = environment.Role,
            Message = message.Text.Replace(Environment.NewLine, "@($NL$)@"),
            ExceptionStacktrace = message.Exception?.StackTrace,
            ExceptionMessage = message.Exception?.GetFullMessage(),
            Source = message.Section,
            Timestamp = message.Time,
            Configuration = environment.Configuration,
            Build = environment.Build,
            Fields = new Dictionary<string, string>
            {
                {nameof(Message.ThreadId), message.ThreadId.ToString()},
                {"Dotnet", sDotnetVersion}
            }
        };
}