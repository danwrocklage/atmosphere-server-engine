using System.Text;
using ACore.Abstractions;
using ACore.Abstractions.Extensions;
using ACore.Abstractions.Logging;
using AUtils.IoC;

namespace ACore.Logging.Providers;

internal class FileProvider : ILoggerProvider
{
    private readonly string mFile;
    private readonly Encoding mEncoding = Encoding.UTF8;
    private DateTime mCurrentDate;

    public FileProvider(Uri url, IContainer container)
    {
        mCurrentDate = DateTime.MinValue;

        if (!string.Equals(url.Scheme, "file", StringComparison.InvariantCultureIgnoreCase))
            throw new ArgumentException($"Only 'file' scheme is supported for {nameof(url)}");
        
        var logDir = Path.GetDirectoryName(url.LocalPath);

        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);

        var environment = container.Resolve<ICellEnvironment>();

        var fileName =
            $"{Path.GetFileNameWithoutExtension(url.LocalPath)}.{environment.Role}.{environment.Configuration}{Path.GetExtension(url.LocalPath)}";
        mFile = Path.Combine(logDir, fileName);

        if (File.Exists(mFile))
#if !DEBUG
            File.Move(mFile, Path.Combine(logDir, $"session_{fileName}_{DateTime.Now:dd-MMM-yy_hh-mm}.log"), true);
#else
            File.Move(mFile, Path.Combine(logDir, $"last_{fileName}"), true);
#endif

        SaveWrite(
            $"Atmosphere Server Engine log{Environment.NewLine}{Environment.NewLine}").GetAwaiter().GetResult();
    }
    
    public LogLevel MinLogLevel { get; set; }

    public async Task Write(Message message)
    {
        if(message.Level < MinLogLevel)
            return;
        
        var today = DateTime.Today;
        if (today != mCurrentDate)
        {
            mCurrentDate = today;
            await SaveWrite($"[{mCurrentDate.ToLongDateString()}]{Environment.NewLine}");
        }
        var msg = message.Format() + Environment.NewLine;

        if (message.Exception != null)
        {
            msg +=
                $"                    Exception: {message.Exception.GetFullMessage()}.{Environment.NewLine}{message.Exception.StackTrace}{Environment.NewLine}";
        }
            
        await SaveWrite(msg);
    }

    public async Task Write(string message)
    {
        await SaveWrite(message + Environment.NewLine);
    }

    private async Task SaveWrite(string message)
    {
        bool success;
        var counts = 0;
        do
        {
            try
            {
                await File.AppendAllTextAsync(mFile, message, mEncoding)
                    .ConfigureAwait(false);
                success = true;
            }
            catch (Exception)
            {
                success = false;
                counts++;
            }
        } while (!success && counts < 20);
    }
}