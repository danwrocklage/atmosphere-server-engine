using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.Json;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Telemetry;
using Prometheus.Client;

namespace ACore.Application.Commands;

/// <summary>
/// Http listener for control command
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
[Log(Category = "Http.Control")]
internal class HttpCommandListener
{
    private const string JSON_CONTENT_TYPE = "application/json";
    private const string SERVER_NAME = "AEngine-HTTP-Control";
    private const string HEALTH_CHECK_ENDPOINT = "hc";
    private const string COMMAND_LIST_ENDPOINT = "cmd";
    private const string METRICS_ENDPOINT = "metrics";

    private static readonly ReadOnlyMemory<byte> sHcOkBuffer = "ok"u8.ToArray();

    private readonly bool mIsEnabled;
    private readonly HttpListener mListener;
    private readonly ICellMetrics mMetrics;
    private readonly ILogger<HttpCommandListener> mLogger;
    private readonly Dictionary<string, ICommandHandler> mHandlers;

    public HttpCommandListener(
        ILogger<HttpCommandListener> logger, 
        IEnumerable<ICommandHandler> handlers,
        IConfiguration configuration,
        ICellMetrics metrics)
    {
        mIsEnabled = configuration.Get("http.control", () => true);
        
        mLogger = logger;
        mMetrics = metrics;
        
        if(!mIsEnabled)
            return;
        
        mListener = new HttpListener();
        
        // If we run app in host OS (local dev build), we don't care about listening port
#if !CONTAINER
        var port = GetFirstAvailablePort();
#else
        var port = 6000;
#endif

        mListener.Prefixes.Add($"http://*:{port}/ctrl/");
        mHandlers = handlers.ToDictionary(
            x => x.GetType().GetCustomAttribute<DisplayNameAttribute>()?.DisplayName, 
            x => x);

        mMetrics.Create("cell_http_command_run", MetricsType.Counter, labels: "command");
        mMetrics.Create("cell_http_command_error", MetricsType.Counter, labels: "command");
    }

    public async Task Run(CancellationToken token)
    {
        if(!mIsEnabled)
            return;
        
        mLogger.Info($"Start listening on {mListener.Prefixes.First()}");
        mListener.Start();

        token.Register(() =>
        {
            mLogger.Debug("Stop listening");
            mListener.Stop();
        });

        await Listen(token)
            .ContinueWith(t =>
            {
                if (t.Exception == null)
                    return;

                if (t.Exception.InnerExceptions.OfType<HttpListenerException>().SingleOrDefault() != null)
                    return;

                mLogger.Error("Error when listening", t.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted)
            .ConfigureAwait(false);
    }

    private async Task Listen(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        while (!token.IsCancellationRequested)
        {
            var context = await mListener.GetContextAsync();
            context.Response.Headers.Set(HttpResponseHeader.Server, SERVER_NAME);

            var command = context.Request.Url?.Segments[^1];
            if (context.Request.ContentLength64 > 0 || 
                context.Request.HttpMethod != "GET" ||
                string.IsNullOrEmpty(command))
            {
                mLogger.Debug($"Got invalid request [{context.Request.HttpMethod}]: {context.Request.Url}");
                context.Response.StatusCode = 400;
                context.Response.Close();
            }


            if (command == HEALTH_CHECK_ENDPOINT)
            {
                await context.Response.OutputStream.WriteAsync(sHcOkBuffer, token);
                context.Response.Close();
                continue;
            }

            if (command == COMMAND_LIST_ENDPOINT)
            {
                context.Response.ContentType = JSON_CONTENT_TYPE;
                await JsonSerializer.SerializeAsync(context.Response.OutputStream, mHandlers.Keys, cancellationToken: token);
                context.Response.Close();
                continue;
            }

            if (command == METRICS_ENDPOINT)
            {
                context.Response.ContentType = "text/plain";
                await ScrapeHandler.ProcessAsync(Metrics.DefaultCollectorRegistry, context.Response.OutputStream, token);
                context.Response.Close();
                continue;
            }
                
            mLogger.Debug($"Got request: {context.Request.Url}");

            if (!string.IsNullOrEmpty(command) && mHandlers.ContainsKey(command))
            {
                try
                {
                    mMetrics.Get("cell_http_command_run").Inc(command);
                    var response = await mHandlers[command].Run(context.Request.QueryString, token);
                    if (response != null)
                    {
                        context.Response.ContentType = JSON_CONTENT_TYPE;
                        await JsonSerializer.SerializeAsync(context.Response.OutputStream, response, cancellationToken: token);
                    }

                    context.Response.StatusCode = 200;
                }
                catch (Exception e)
                {
                    mMetrics.Get("cell_http_command_error").Inc(command);
                    mLogger.Error($"Error on {context.Request.Url}", e);
                    context.Response.StatusCode = 500;
                }
            }
            else
                context.Response.StatusCode = 404;

            context.Response.Close();
        }
    }
    
    private static int GetFirstAvailablePort()
    {
        const int cMinDynamicPort = 49250;
        
        var ipProps = IPGlobalProperties.GetIPGlobalProperties();
        var activePorts = ipProps.GetActiveTcpListeners()
            .Concat(ipProps.GetActiveUdpListeners())
            .Where(x => x.Port >= cMinDynamicPort)
            .Select(x => x.Port).ToHashSet();
        
        var port = cMinDynamicPort;
        while (activePorts.Contains(port))
            port++;

        if (port > ushort.MaxValue)
            throw new Exception($"All dynamic ports ({cMinDynamicPort}-{ushort.MaxValue}) are busy");

        return port;
    }
}