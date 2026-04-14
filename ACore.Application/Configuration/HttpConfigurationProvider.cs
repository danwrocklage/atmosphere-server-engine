using System.Net.Http.Json;

namespace ACore.Application.Configuration;

internal record ConfigResponse(string Json, string[] Modules);

/// <summary>
/// Get application configuration from HTTP configuration server
/// </summary>
internal class HttpConfigurationProvider : IConfigurationProvider
{
    private readonly CommandLineArgs mArgsInfo;
    private readonly HttpClient mHttpClient;
    private readonly Uri mRequestPath;
    
    public HttpConfigurationProvider(CommandLineArgs argsInfo)
    {
        mArgsInfo = argsInfo;
        mRequestPath = new Uri($"/config/{mArgsInfo.Role}.{mArgsInfo.Configuration}", UriKind.Relative);
        mHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5), 
            BaseAddress = argsInfo.ConfigurationPath,
            DefaultRequestHeaders = { {"Authentication", argsInfo.AccessToken} }
        };
    }

    /// <inheritdoc />
    public async Task<CellBuildConfiguration> Get(CancellationToken token = default)
    {
        using var response = await mHttpClient.GetAsync(mRequestPath, token);
        var (json, modules) = await response.Content.ReadFromJsonAsync<ConfigResponse>(cancellationToken: token);
        
        return new CellBuildConfiguration
        {
            Configuration = mArgsInfo.Configuration,
            Modules = modules,
            Role = mArgsInfo.Role,
            Build = mArgsInfo.Build,
            JsonPayload = json
        };
    }
}