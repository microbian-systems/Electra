using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Core;

public class AuthenticationTests : ProviderTestBase
{
    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        var provider = new TestOAuth2Provider(HttpClient, LoggerMock.Object, ConfigurationMock.Object);
        
        var result = await provider.GenerateAuthUrlAsync();
        
        result.Url.ShouldNotBeNullOrEmpty();
        result.Url.ShouldContain("https://test.com/oauth/authorize");
        result.Url.ShouldContain("client_id=");
        result.Url.ShouldContain("redirect_uri=");
        result.Url.ShouldContain("response_type=code");
        result.Url.ShouldContain("scope=");
        result.State.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldExchangeCodeForToken()
    {
        HttpHandler.WhenPost("*token*")
            .RespondWith(MockResponses.OAuth2.TokenResponse("test_access_token", "test_refresh_token"));
        
        HttpHandler.WhenGet("*userinfo*")
            .RespondWith("{\"id\": \"123\", \"name\": \"Test User\"}");

        var provider = new TestOAuth2Provider(HttpClient, LoggerMock.Object, ConfigurationMock.Object);
        var parameters = new AuthenticateParams("auth_code", "code_verifier");
        
        var result = await provider.AuthenticateAsync(parameters);
        
        result.AccessToken.ShouldBe("test_access_token");
        result.RefreshToken.ShouldBe("test_refresh_token");
        result.Id.ShouldBe("123");
        result.Name.ShouldBe("Test User");
    }

    [Fact]
    public async Task AuthenticateAsync_OnError_ShouldThrowException()
    {
        HttpHandler.WhenPost("*token*")
            .RespondWith(MockResponses.OAuth2.ErrorResponse("invalid_grant", "Invalid authorization code"), 
                HttpStatusCode.BadRequest);

        var provider = new TestOAuth2Provider(HttpClient, LoggerMock.Object, ConfigurationMock.Object);
        var parameters = new AuthenticateParams("invalid_code", "code_verifier");
        
        await Should.ThrowAsync<Exception>(() => provider.AuthenticateAsync(parameters));
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNewToken()
    {
        HttpHandler.WhenPost("*token*")
            .RespondWith(MockResponses.OAuth2.TokenResponse("new_access_token", "new_refresh_token"));

        var provider = new TestOAuth2Provider(HttpClient, LoggerMock.Object, ConfigurationMock.Object);
        
        var result = await provider.RefreshTokenAsync("old_refresh_token");
        
        result.AccessToken.ShouldBe("new_access_token");
        result.RefreshToken.ShouldBe("new_refresh_token");
    }

    [Fact]
    public async Task AuthenticateAsync_WithPKCE_ShouldIncludeCodeVerifier()
    {
        var receivedContent = "";
        HttpHandler.WhenPost("*token*")
            .RespondWith((req) =>
            {
                if (req.Content != null)
                {
                    var task = req.Content.ReadAsStringAsync();
                    task.Wait();
                    receivedContent = task.Result;
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(MockResponses.OAuth2.TokenResponse("access_token"))
                };
            });
        
        HttpHandler.WhenGet("*userinfo*")
            .RespondWith("{\"id\": \"123\", \"name\": \"Test User\"}");

        var provider = new TestOAuth2Provider(HttpClient, LoggerMock.Object, ConfigurationMock.Object);
        var parameters = new AuthenticateParams("auth_code", "my_code_verifier");
        
        await provider.AuthenticateAsync(parameters);
        
        receivedContent.ShouldContain("code_verifier=my_code_verifier");
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_WithClientInformation_ShouldUseCustomSettings()
    {
        var provider = new TestOAuth2Provider(HttpClient, LoggerMock.Object, ConfigurationMock.Object);
        var clientInfo = new ClientInformation
        {
            ClientId = "custom_client_id",
            ClientSecret = "custom_secret",
            InstanceUrl = "https://custom.com"
        };
        
        var result = await provider.GenerateAuthUrlAsync(clientInfo);
        
        result.Url.ShouldContain("client_id=custom_client_id");
    }
}

public class TestOAuth2Provider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public TestOAuth2Provider(HttpClient httpClient, ILogger logger, IConfiguration configuration) 
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override string Identifier => "test-oauth2";
    public override string Name => "Test OAuth2 Provider";
    public override string[] Scopes => new[] { "read", "write" };

    public override int MaxLength(object? additionalSettings = null) => 1000;

    protected string GetClientId() => _configuration["TEST_CLIENT_ID"] ?? "default_client_id";
    protected string GetClientSecret() => _configuration["TEST_CLIENT_SECRET"] ?? "default_secret";
    protected string GetRedirectUri() => _configuration["TEST_REDIRECT_URI"] ?? "https://localhost/callback";

    public override async Task<PostResponse[]> PostAsync(
        string id, string accessToken, List<PostDetails> posts, 
        Integration integration, CancellationToken cancellationToken = default)
    {
        return Array.Empty<PostResponse>();
    }

    public override Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(6);
        var clientId = clientInformation?.ClientId ?? GetClientId();
        var redirectUri = GetRedirectUri();

        var url = $"https://test.com/oauth/authorize" +
                  $"?client_id={clientId}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&response_type=code" +
                  $"&scope={string.Join(" ", Scopes)}" +
                  $"&state={state}";

        return Task.FromResult(new GenerateAuthUrlResponse
        {
            Url = url,
            State = state,
            CodeVerifier = MakeId(10)
        });
    }

    public override async Task<AuthTokenDetails> AuthenticateAsync(
        AuthenticateParams parameters,
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var clientId = clientInformation?.ClientId ?? GetClientId();
        var clientSecret = clientInformation?.ClientSecret ?? GetClientSecret();
        var redirectUri = GetRedirectUri();

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://test.com/oauth/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", parameters.Code },
                { "redirect_uri", redirectUri },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "code_verifier", parameters.CodeVerifier }
            })
        };

        var tokenResponse = await HttpClient.SendAsync(tokenRequest, cancellationToken);
        var tokenContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Token request failed: {tokenContent}");
        }

        var tokenData = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(tokenContent);

        var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://test.com/oauth/userinfo");
        userRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData!.access_token);
        
        var userResponse = await HttpClient.SendAsync(userRequest, cancellationToken);
        var userContent = await userResponse.Content.ReadAsStringAsync(cancellationToken);
        var userData = System.Text.Json.JsonSerializer.Deserialize<UserResponse>(userContent);

        return new AuthTokenDetails
        {
            AccessToken = tokenData.access_token,
            RefreshToken = tokenData.refresh_token ?? "",
            ExpiresIn = tokenData.expires_in,
            Id = userData!.id,
            Name = userData.name ?? ""
        };
    }

    public override async Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://test.com/oauth/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "client_id", GetClientId() },
                { "client_secret", GetClientSecret() }
            })
        };

        var tokenResponse = await HttpClient.SendAsync(tokenRequest, cancellationToken);
        var tokenContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        var tokenData = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(tokenContent);

        return new AuthTokenDetails
        {
            AccessToken = tokenData!.access_token,
            RefreshToken = tokenData.refresh_token ?? "",
            ExpiresIn = tokenData.expires_in
        };
    }

    private class TokenResponse
    {
        public string access_token { get; set; } = "";
        public string? refresh_token { get; set; }
        public int expires_in { get; set; }
    }

    private class UserResponse
    {
        public string id { get; set; } = "";
        public string? name { get; set; }
    }
}
