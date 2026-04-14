using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("ACore.Application")]
[assembly:InternalsVisibleTo("ACore.Configuration.Module")]
[assembly:InternalsVisibleTo("ACore.Logging.Module")]
[assembly:InternalsVisibleTo("ACore.Modules")]

// ReSharper disable once CheckNamespace
namespace ACore.Application;

/// <summary>
/// Simple console logger only for debug purposes
/// </summary>
internal static class DebugLogger
{
    /// <summary>
    /// Show <see cref="message"/> in the console output
    /// </summary>
    public static void WriteLine(string message, ConsoleColor color = ConsoleColor.Gray)
    {
        Debug.WriteLine($"Cell: {message}");
#if DEBUG
        var old = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = old;
#endif
    }
}