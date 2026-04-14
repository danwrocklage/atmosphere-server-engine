using System.ComponentModel;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Web;
using AUtils.MethodExec;
using IContainer = AUtils.IoC.IContainer;

namespace ACore.Worker.Web.Routing;

/// <summary>
/// Route delegate description
/// </summary>
/// <param name="Action">Controller method</param>
/// <param name="Parameters">Route arguments mapped to method</param>
internal record RouteDescription(MethodInfo Action,
    IReadOnlyDictionary<string, (Type, ActionParameterType?)> Parameters);
    
/// <summary>
/// Request endpoint executor
/// </summary>
internal class ActionHandler
{
    private readonly IReadOnlyDictionary<string, (Type, ActionParameterType?)> mArgs;
    private readonly IContainer mContainer;
    private readonly ObjectMethodExecutor mMethodExecutor;

    public ActionHandler(MethodInfo action, Type controllerType, IContainer container)
    {
        mContainer = container;
        mArgs = ActionParameterUtility.GetParametersMeta(action.GetParameters());
        mMethodExecutor = ObjectMethodExecutor.Create(action, controllerType.GetTypeInfo());
    }

    /// <summary>
    /// Get endpoint CLR description
    /// </summary>
    public RouteDescription Description => new(mMethodExecutor.MethodInfo, mArgs);

    /// <summary>
    /// Run current endpoint executor
    /// </summary>
    /// <param name="urlParams">Endpoint parameters</param>
    /// <param name="context">Http context</param>
    /// <param name="session">Request temp storage</param>
    public async Task Handle(IReadOnlyDictionary<string, string> urlParams, HttpListenerContext context, Session session, CancellationToken token)
    {
        var args = await GetArgs(urlParams, context, token);
        await RunAction(context, session, args);
    }

    /// <summary>
    /// Execute handler
    /// </summary>
    private async Task RunAction(HttpListenerContext context, Session session, object[] args)
    {
        var controller = (Controller) mContainer.Resolve(mMethodExecutor.TargetTypeInfo.AsType());
        controller.Context = context;
        controller.Session = session;

        var r = mMethodExecutor.IsMethodAsync
            ? await mMethodExecutor.ExecuteAsync(controller, args)
            : mMethodExecutor.Execute(controller, args);
            
        if (r != null)
            await JsonSerializer.SerializeAsync(context.Response.OutputStream, r);

        if (!controller.StatusCodeWasChanged)
            context.Response.StatusCode = r == null ? 204 : 200;
    }

    /// <summary>
    /// Prepare method arguments from url parameters and context
    /// </summary>
    private async Task<object[]> GetArgs(IReadOnlyDictionary<string, string> urlParams, HttpListenerContext context,
        CancellationToken cancellationToken)
    {
        var args = new object[mArgs.Count];
        short i = 0;
        var queryParams = ParseQuery(context.Request.Url?.Query);
        foreach (var (name, (type, parameterType)) in mArgs)
        {
            switch (parameterType)
            {
                case ActionParameterType.Body:
                    if (context.Request.HasEntityBody)
                        args[i] = await JsonSerializer.DeserializeAsync(context.Request.InputStream, type);
                    else
                        args[i] = type.IsValueType ? Activator.CreateInstance(type) : null;
                    break;
                case ActionParameterType.Query when queryParams.TryGetValue(name, out var queryValue1):
                    SetArgValue(queryValue1, type, args, i, true);
                    break;
                case ActionParameterType.Route when urlParams.TryGetValue(name, out var routeValue1):
                    SetArgValue(routeValue1, type, args, i, true);
                    break;
                case ActionParameterType.Service:
                    args[i] = mContainer.Resolve(type);
                    break;
                case ActionParameterType.Header:
                    SetArgValue(context.Request.Headers.Get(name), type, args, i, false);
                    break;
                case ActionParameterType.Stream:
                    args[i] = context.Request.InputStream;
                    break;
                case ActionParameterType.CancellationToken:
                    args[i] = cancellationToken;
                    break;
                case null:
                    if(urlParams.TryGetValue(name, out var routeValue2))
                        SetArgValue(routeValue2, type, args, i, true);
                    else if(queryParams.TryGetValue(name, out var queryValue2))
                        SetArgValue(queryValue2, type, args, i, true);
                    break;
                default:
                    args[i] = type.IsValueType ? Activator.CreateInstance(type) : null;
                    break;
            }

            i++;
        }

        return args;
    }

    private static void SetArgValue(string queryValue, Type type, object[] args, short i, bool urlDecode)
    {
        var decodedValue = urlDecode ? HttpUtility.HtmlDecode(queryValue) : queryValue;
        var converter = TypeDescriptor.GetConverter(type);
        args[i] = converter.ConvertFromString(decodedValue);
    }

    private static Dictionary<string, string> ParseQuery(string urlQuery)
    {
        var result = new Dictionary<string, string>();
            
        if(string.IsNullOrEmpty(urlQuery))
            return result;
            
        var parameters = urlQuery
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        foreach (var parameter in parameters)
        {
            var keyValuePair = parameter.Split('=', StringSplitOptions.TrimEntries);
            if (keyValuePair.Length != 2) 
                continue;
                
            if(!string.IsNullOrEmpty(keyValuePair[0]))
                result.Add(keyValuePair[0], keyValuePair[1]);
        }

        return result;
    }
}