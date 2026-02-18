using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class InstagramStandaloneProvider : SocialProviderBase
{
    private readonly InstagramProvider _instagramProvider;
    private readonly IConfiguration _configuration;

    public override string Identifier => "instagram-standalone";
    public override string Name => "Instagram\n(Standalone)";
    public override string[] Scopes => new[]
    {
        "instagram_business_basic",
        "instagram_business_content_publish",
        "instagram_business_manage_comments",
        "instagram_business_manage_insights"
    };
    public override int MaxConcurrentJobs => 200;
    public override bool IsBetweenSteps => false;

    public InstagramStandaloneProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<InstagramStandaloneProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
        _instagramProvider = new InstagramProvider(httpClient, configuration,
            new LoggerFactory().CreateLogger<InstagramProvider>());
    }

    public override int MaxLength(object? additionalSettings = null) => 2200;

    protected override ErrorHandlingResult? HandleErrors(string responseBody)
    {
        return _instagramProvider.HandleErrors(responseBody);
    }

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(6);
        var appId = GetAppId();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = $"{frontendUrl}/integrations/social/instagram-standalone";

        if (frontendUrl.StartsWith("http://"))
        {
            redirectUri = $"https://redirectmeto.com/{frontendUrl}/integrations/social/instagram-standalone";
        }

        var url = "https://www.instagram.com/oauth/authorize" +
                  "?enable_fb_login=0" +
                  $"&client_id={appId}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  "&response_type=code" +
                  $"&scope={Uri.EscapeDataString(string.Join(",", Scopes))}" +
                  $"&state={state}";

        return new GenerateAuthUrlResponse
        {
            Url = url,
            CodeVerifier = MakeId(10),
            State = state
        };
    }

    public override async Task<AuthTokenDetails> AuthenticateAsync(
        AuthenticateParams parameters,
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var appId = GetAppId();
        var appSecret = GetAppSecret();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = $"{frontendUrl}/integrations/social/instagram-standalone";

        if (frontendUrl.StartsWith("http://"))
        {
            redirectUri = $"https://redirectmeto.com/{frontendUrl}/integrations/social/instagram-standalone";
        }

        var form = new MultipartFormDataContent
        {
            { new StringContent(appId), "client_id" },
            { new StringContent(appSecret), "client_secret" },
            { new StringContent("authorization_code"), "grant_type" },
            { new StringContent(redirectUri), "redirect_uri" },
            { new StringContent(parameters.Code), "code" }
        };

        var tokenResponse = await HttpClient.PostAsync("https://api.instagram.com/oauth/access_token", form, cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();

        var shortToken = await DeserializeAsync<InstagramShortTokenResponse>(tokenResponse);

        var exchangeUrl = "https://graph.instagram.com/access_token" +
                          "?grant_type=ig_exchange_token" +
                          $"&client_id={appId}" +
                          $"&client_secret={appSecret}" +
                          $"&access_token={shortToken.AccessToken}";

        var exchangeResponse = await HttpClient.GetAsync(exchangeUrl, cancellationToken);
        exchangeResponse.EnsureSuccessStatusCode();

        var longToken = await DeserializeAsync<InstagramLongTokenResponse>(exchangeResponse);

        var userInfo = await GetUserInfoAsync(longToken.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.UserId ?? "",
            Name = userInfo.Name ?? "",
            AccessToken = longToken.AccessToken,
            RefreshToken = longToken.AccessToken,
            ExpiresIn = (int)TimeSpan.FromDays(58).TotalSeconds,
            Picture = userInfo.ProfilePictureUrl ?? string.Empty,
            Username = userInfo.Username ?? ""
        };
    }

    public override async Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://graph.instagram.com/refresh_access_token?grant_type=ig_refresh_token&access_token={refreshToken}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenInfo = await DeserializeAsync<InstagramRefreshTokenResponse>(response);
        var userInfo = await GetUserInfoAsync(tokenInfo.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.UserId ?? "",
            Name = userInfo.Name ?? "",
            AccessToken = tokenInfo.AccessToken,
            RefreshToken = tokenInfo.AccessToken,
            ExpiresIn = (int)TimeSpan.FromDays(58).TotalSeconds,
            Picture = userInfo.ProfilePictureUrl ?? string.Empty,
            Username = userInfo.Username ?? ""
        };
    }

    public override async Task<PostResponse[]> PostAsync(
        string id,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken = default)
    {
        return await _instagramProvider.PostAsync(id, accessToken, posts, integration, cancellationToken);
    }

    public override async Task<PostResponse[]?> CommentAsync(
        string id,
        string postId,
        string? lastCommentId,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken = default)
    {
        return await _instagramProvider.CommentAsync(id, postId, lastCommentId, accessToken, posts, integration, cancellationToken);
    }

    public override async Task<AnalyticsData[]?> AnalyticsAsync(
        string id,
        string accessToken,
        int days,
        CancellationToken cancellationToken = default)
    {
        return await _instagramProvider.AnalyticsAsync(id, accessToken, days, cancellationToken);
    }

    public override async Task<AnalyticsData[]?> PostAnalyticsAsync(
        string integrationId,
        string accessToken,
        string postId,
        int days,
        CancellationToken cancellationToken = default)
    {
        return await _instagramProvider.PostAnalyticsAsync(integrationId, accessToken, postId, days, cancellationToken);
    }

    private async Task<InstagramUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var url = $"https://graph.instagram.com/v21.0/me?fields=user_id,username,name,profile_picture_url&access_token={accessToken}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<InstagramUserInfo>(response);
    }

    private string GetAppId() => _configuration["INSTAGRAM_APP_ID"] ?? throw new InvalidOperationException("INSTAGRAM_APP_ID not configured");
    private string GetAppSecret() => _configuration["INSTAGRAM_APP_SECRET"] ?? throw new InvalidOperationException("INSTAGRAM_APP_SECRET not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    private class InstagramShortTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("permissions")]
        public List<string>? Permissions { get; set; }
    }

    private class InstagramLongTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }

    private class InstagramRefreshTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    private class InstagramUserInfo
    {
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("profile_picture_url")]
        public string? ProfilePictureUrl { get; set; }
    }

    #endregion
}
