using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ACore.Tests.Shared;
using ACore.Worker.Web.Routing;
using ACore.Worker.Web.Routing.Attributes;
using AUtils.IoC;
using Xunit;

namespace ACore.Worker.Web.Tests;

public class Test1Controller : Controller
{
    [Get("action")]
    public void TestMethod()
    {
        Response(204);
    }
}

public class Test2Controller : Controller
{
    
}

public class TestMiddleware1 : Middleware
{
    public override async Task Execute(HttpListenerContext context, Session session, CancellationToken token)
    {
        await Next(context, session, token);
    }
}

public class TestMiddleware2 : Middleware
{
    public override async Task Execute(HttpListenerContext context, Session session, CancellationToken token)
    {
        await Next(context, session, token);
    }
}

public class TestModule : Module
{
    public override void Configure(PipelineBuilder builder)
    {
        builder.UseController<Test2Controller>();
        builder.UseMiddleware<TestMiddleware2>();
    }
}

public class TestWebWorker : WebWorker
{
    public TestWebWorker(IContainer container) : base(container)
    {
    }

    protected override void Configure(PipelineBuilder builder)
    {
        builder.UseRoutingInfo();
        builder.UseController<Test1Controller>();
        builder.UseMiddleware<TestMiddleware1>();
        builder.UseModule<TestModule>();
    }
}

public class WebWorkerTests
{
    private static ContainerBuilder PrepareContainer()
    {
        var builder = new ContainerBuilder();
        builder.AddFakeServices();
        var module = new WebWorkerModule();
        module.ConfigureServices(builder);
        return builder;
    }

    [Fact]
    public void BuildDependencyContainerTest()
    {
        var builder = PrepareContainer();
        Assert.True(builder.IsRegistered<Test1Controller>());
        Assert.True(builder.IsRegistered<TestMiddleware1>());
        Assert.True(builder.IsRegistered<TestModule>());
        builder.Transient<TestWebWorker>();
        var worker = builder.Build().Resolve<TestWebWorker>();
        Assert.NotNull(worker);
    }

    
    [Fact]
    public async Task RunWebWorkerTest()
    {
        var builder = PrepareContainer();
        builder.Transient<TestWebWorker>();
        var worker = builder.Build().Resolve<TestWebWorker>();

        var cts = new CancellationTokenSource(100);
        await worker.Run(cts.Token);
    }
    
    [Fact]
    public async Task SimpleGetRequestTest()
    {
        var builder = PrepareContainer();
        builder.Transient<TestWebWorker>();
        var worker = builder.Build().Resolve<TestWebWorker>();

        var cts = new CancellationTokenSource(100);
        worker.RunNonBlocking(cts.Token);
        
        Assert.False(cts.IsCancellationRequested);

        var client = new HttpClient();
        client.BaseAddress = new Uri("http:\\localhost:5000");

        var response = await client.GetAsync("api/test1/action");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}