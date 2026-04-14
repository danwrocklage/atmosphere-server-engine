using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Application;
using IContainer = AUtils.IoC.IContainer;

namespace ACore.Logging.Providers;

/// <summary>
/// Provider, that writes to logstash
/// </summary>
[Description("logstash")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
internal class LogstashProvider : ILoggerProvider, IDisposable
{
    private readonly ICellEnvironment mCellEnvironment;
    private readonly bool mIsEnabled;
    private readonly TcpClient mClient;
        
    public LogstashProvider(Uri url, IContainer container)
    {
        if(!string.Equals(url.Scheme, "tcp", StringComparison.InvariantCultureIgnoreCase))
            throw new ArgumentException($"Only 'file' scheme is supported for {nameof(url)}");

        try
        {
            mClient = new TcpClient(url.Host, url.Port);
            mIsEnabled = true;
        }
        catch (Exception e)
        {
            DebugLogger.WriteLine($"Failed to connect to '{url}' in {nameof(LogstashProvider)} ({e.Message})", ConsoleColor.Red);
            mIsEnabled = false;
        }
        mCellEnvironment = container.Resolve<ICellEnvironment>();
    }

    public LogLevel MinLogLevel { get; set; }

    public async Task Write(Message message)
    {
        if(!mIsEnabled || message.Level < MinLogLevel)
            return;

        var json = JsonSerializer.Serialize(message.ToEvent(mCellEnvironment));
            
        var buffer = Encoding.UTF8.GetBytes($"{json}{Environment.NewLine}");
        await mClient.GetStream().WriteAsync(buffer);
    }

    public Task Write(string message) => Task.CompletedTask;
    
    public void Dispose()
    {
        mClient?.Dispose();
    }
}