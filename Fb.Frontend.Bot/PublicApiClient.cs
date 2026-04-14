using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
// ReSharper disable ClassNeverInstantiated.Local

namespace Fb.Frontend.Bot;

[Log(Category = "[api] public")]
internal class PublicApiClient : IDisposable
{
    private readonly HttpClient mHttpClient;
    private readonly AccountConfiguration mAccountConfiguration;
    private readonly ILogger<PublicApiClient> mLogger;

    public PublicApiClient(ILogger<PublicApiClient> logger, IConfiguration configuration)
    {
        mAccountConfiguration = configuration.Get<AccountConfiguration>(() => null!);
        if (mAccountConfiguration == null)
            throw new ApplicationException("There is no account configuration");
        
        var publicApiConfiguration = configuration.Get<PublicApiConfiguration>(() => null!);
        if(publicApiConfiguration == null)
            throw new ApplicationException("There is no public api configuration");
        
        if(string.IsNullOrEmpty(publicApiConfiguration.Url))
            throw new ApplicationException("Public api URL is null or empty");

        mLogger = logger;
        mHttpClient = new HttpClient
        {
            BaseAddress = new Uri(publicApiConfiguration.Url), 
            Timeout = publicApiConfiguration.TimeoutSeconds
        };
    }

    public async Task Login(CancellationToken cancellationToken = default)
    {
        mLogger.Debug("Authentication...");
        var tokenResponse = await mHttpClient.PostAsync("api/auth/token", new ByteArrayContent(
            JsonSerializer.SerializeToUtf8Bytes(new
            {
                mAccountConfiguration.Login,
                mAccountConfiguration.Password,
                GrandType = "web_auth"
            })), cancellationToken);
        if (tokenResponse.StatusCode == HttpStatusCode.BadRequest)
        {
            mLogger.Debug("Authentication failed. Try to create account");
            await CreateAccount(cancellationToken);
            tokenResponse = await mHttpClient.PostAsync("api/auth/token", new ByteArrayContent(
                JsonSerializer.SerializeToUtf8Bytes(new
                {
                    mAccountConfiguration.Login,
                    mAccountConfiguration.Password,
                    GrandType = "web_auth"
                })), cancellationToken);
        }

        if (!tokenResponse.IsSuccessStatusCode)
            throw new ApplicationException("Failed to authenticate");
    
        var jwt = (await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken)).GetProperty("Token").GetString();
        mHttpClient.DefaultRequestHeaders.Add("Authorization", jwt);
        mLogger.Success("Authentication succeeded");
    }

    public async Task<string?> GetGameToken(CancellationToken cancellationToken = default)
    {
        mLogger.Debug("Queue to game server...");
        var gameTokenResponse = await mHttpClient.PostAsync("api/game/prepare", new ByteArrayContent(Array.Empty<byte>()), cancellationToken);
        if(gameTokenResponse.StatusCode == HttpStatusCode.NotAcceptable)
            throw new ApplicationException("Frontend servers are not running");
        
        var json = await gameTokenResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        var gameToken = json.GetProperty("Token").GetString();
        if (string.IsNullOrEmpty(gameToken))
            throw new ApplicationException("");
        
        mLogger.Success("Game preparation succeeded");
        return gameToken;
    }

    private async Task CreateAccount(CancellationToken cancellationToken = default)
    {
        mLogger.Debug("Creating account...");
        var response = await mHttpClient.PostAsync("api/account/new", new ByteArrayContent(
            JsonSerializer.SerializeToUtf8Bytes(mAccountConfiguration)), cancellationToken);
        
        // In development build public api responses activation code
        var activationToken = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        mLogger.Success("Account creation succeeded. Activating it");

        // Activate account
        await mHttpClient.PostAsync("api/account/activate", new ByteArrayContent(
            JsonSerializer.SerializeToUtf8Bytes(new
            {
                Code = activationToken.GetProperty("ActivationToken").GetString()
            })), cancellationToken);
        mLogger.Success("Account activation succeeded");
    }

    #region Utils

    [Configuration("public.api")]
    private class PublicApiConfiguration
    {
        public string? Url { get; set; }
        
        public int Timeout { get; set; }
        
        public TimeSpan TimeoutSeconds => TimeSpan.FromSeconds(Timeout);
    }
    
    [Configuration("account")]
    private class AccountConfiguration
    {
        public string? Login { get; set; }
        
        public string? Password { get; set; }
        
        public string? Name { get; set; }
        
        public string? Email { get; set; }
    }

    #endregion

    public void Dispose()
    {
        mHttpClient.Dispose();
    }
}