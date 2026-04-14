using System.Net;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Telemetry;
using ACore.Worker.Web.Routing;
using AUtils.IoC;

namespace ACore.Worker.Web;

/// <summary>
/// Worker base for processing http requests
/// </summary>
[Log(Category = "http.web")]
public abstract class WebWorker : IRunnable
{
    private readonly HttpListener mListener;
    private readonly ILogger<WebWorker> mLogger;
    private readonly PipelineBuilder mPipelineBuilder;
    private readonly string mListeningPath;
    private readonly ICellMetrics mMetrics;

    protected WebWorker(IContainer container)
    {
        mLogger = container.Resolve<ILogger<WebWorker>>();
        mPipelineBuilder = container.Resolve<PipelineBuilder>();
        mMetrics = container.Resolve<ICellMetrics>();

        var config = container.Resolve<IConfiguration>().Get(() => WebWorkerConfig.Default);
        mListeningPath = $"http://*:{config.PortOut.ToString()}/{config.Path}/";
        mListener = new HttpListener();
        mListener.Prefixes.Clear();
        mListener.Prefixes.Add(mListeningPath);
    }

    /// <summary>
    /// Run http requests processing pipeline
    /// </summary>
    public async Task Run(CancellationToken token)
    {
        var pipeline = PrepareRunning(token);

        await Task.Run(() => Listen(pipeline, token), token).ContinueWith(t =>
        {
            if (t.Exception == null) 
                return;
                
            if (t.Exception.InnerExceptions.OfType<HttpListenerException>().SingleOrDefault() != null)
                return;

            mLogger.Error("Error when listening", t.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
    
    /// <summary>
    /// Run http requests processing pipeline
    /// </summary>
    internal void RunNonBlocking(CancellationToken token)
    {
        var pipeline = PrepareRunning(token);

        _ = Task.Run(() => Listen(pipeline, token), token).ContinueWith(t =>
        {
            if (t.Exception == null) 
                return;
                
            if (t.Exception.InnerExceptions.OfType<HttpListenerException>().SingleOrDefault() != null)
                return;

            mLogger.Error("Error when listening", t.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    private Pipeline PrepareRunning(CancellationToken token)
    {
        Configure(mPipelineBuilder);
        CreateMetrics();
        var pipeline = mPipelineBuilder.Build();

        mLogger.Info($"Start listening on {mListeningPath}");
        mListener.Start();
        token.Register(() => { mListener.Stop(); });
        return pipeline;
    }

    private void CreateMetrics()
    {
        mMetrics.Create("cell_http_request_count", MetricsType.Counter, "Total http requests count", "url",
            "cell_role");
        mMetrics.Create("cell_http_request_fail_count", MetricsType.Counter, "Total failed http requests count",
            "url", "cell_role");
    }

    private async Task Listen(Pipeline pipeline, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var context = await mListener.GetContextAsync();
            await pipeline.Execute(context, token);
        }
    }

    /// <summary>
    /// Configure http request processing pipeline 
    /// </summary>
    protected abstract void Configure(PipelineBuilder builder);
}