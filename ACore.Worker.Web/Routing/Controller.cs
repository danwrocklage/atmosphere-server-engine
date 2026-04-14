using System.Net;
using System.Text.Json;

namespace ACore.Worker.Web.Routing;

public abstract class Controller
{
    private bool mIsOutputWrote;
        
    internal HttpListenerContext Context { get; set; }
        
    internal bool StatusCodeWasChanged { get; private set; }

    protected HttpListenerRequest Request => Context.Request;
        
    protected internal Session Session { get; internal set; }

    protected async Task Response<T>(T payload, int statusCode = 200)
    {
        if (mIsOutputWrote)
            throw new InvalidOperationException();
            
        Context.Response.StatusCode = statusCode;
        // TODO: Fix sending whole response
        Context.Response.SendChunked = true;
        await JsonSerializer.SerializeAsync(Context.Response.OutputStream, payload);
        StatusCodeWasChanged = true;
        mIsOutputWrote = true;
    }

    protected async Task Response(Stream output, int statusCode = 200)
    {
        if (output == null) 
            throw new ArgumentNullException(nameof(output));
        
        if (mIsOutputWrote || !output.CanRead)
            throw new InvalidOperationException();
        
        Context.Response.StatusCode = statusCode;
        // TODO: Fix sending whole response
        Context.Response.SendChunked = true;
        await output.CopyToAsync(Context.Response.OutputStream);
        StatusCodeWasChanged = true;
        mIsOutputWrote = true;
    }
        
    protected void Response(int statusCode = 200)
    {
        Context.Response.StatusCode = statusCode;
        StatusCodeWasChanged = true;
    }
}