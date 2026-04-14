using System.Runtime.CompilerServices;
using ACore.Abstractions.Extensions;
using ACore.Abstractions.Logging;

namespace ACore.Logging.Providers;

internal class ConsoleProvider : ILoggerProvider
{
    private Message mLastMessage;
    private int mLastCursor;
    private int mRepeatCount;

    public LogLevel MinLogLevel { get; set; }

    public Task Write(Message message)
    {
        if (message.Level < MinLogLevel)
            return Task.CompletedTask;
        
        StartColoredConsole(GetColor(message.Level));
        if (message == mLastMessage)
        {
            mRepeatCount++;
            Console.SetCursorPosition(0, mLastCursor);
            Console.WriteLine($"[{mRepeatCount.ToString(),3}]{message.Format()}");
        }
        else
        {
            mLastCursor = Console.CursorTop;
            mLastMessage = message;
            mRepeatCount = 1;
            Console.WriteLine($"     {message.Format()}");
        }

        if (message.Exception != null)
        {
            Console.WriteLine($"{"Exception",41}: {message.Exception.GetFullMessage()}");
#if DEBUG
            Console.WriteLine($"{"Stack trace",41}: {message.Exception.StackTrace}");
#endif
        }
        EndColoredConsole();

        return Task.CompletedTask;
    }

    public Task Write(string message)
    {
        StartColoredConsole(ConsoleColor.DarkBlue);
        Console.WriteLine(message);
        EndColoredConsole();

        return Task.CompletedTask;
    }

    private static ConsoleColor GetColor(LogLevel messageLevel) =>
        messageLevel switch
        {
            LogLevel.Debug => ConsoleColor.DarkGray,
            LogLevel.Info => ConsoleColor.Gray,
            LogLevel.Success => ConsoleColor.DarkCyan,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Fatal => ConsoleColor.DarkRed,
            _ => Console.ForegroundColor
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void StartColoredConsole(ConsoleColor color)
    {
#if DEBUG
        Console.ForegroundColor = color;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EndColoredConsole()
    {
#if DEBUG
        Console.ResetColor();
#endif
    }

    #region Header

    internal static void ShowHeader()
    {
        StartColoredConsole(ConsoleColor.DarkBlue);
        Console.WriteLine(@"     _   _                             _                   ");
        Console.WriteLine(@"    / \ | |_ _ __ ___   ___  ___ _ __ | |__   ___ _ __ ___ ");
        Console.WriteLine(@"   / _ \| __| '_ \` _ \ / _ \/ __| '_ \| '_ \ / _ | '__/ _ \");
        Console.WriteLine(@"  / ___ | |_| | | | | | (_) \__ | |_) | | | |  __| | |  __/");
        Console.WriteLine(@" /_/___\_\__|_| |_| |_|\___/|___| .__/|_| |_|\___|_|  \___|");
        Console.WriteLine(@" | ____|_ __   __ _(_)_ __   ___|_|                        ");
        Console.WriteLine(@" |  _| | '_ \ / _\` | | '_ \ / _ \                          ");
        Console.WriteLine(@" | |___| | | | (_| | | | | |  __/                          ");
        Console.WriteLine(@" |_____|_| |_|\__, |_|_| |_|\___|                          ");
        Console.WriteLine(@"              |___/                                        ");
        Console.WriteLine();
        EndColoredConsole();
    }

    #endregion
}