using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class YouTubeProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "youtube";
    public override string Name => "YouTube";
    public override bool IsBetweenSteps => true;
    public override string[] Scopes => new[]
    {
        "https://www.googleapis.com/auth/userinfo.profile",
        "https://www.googleapis.com/auth/userinfo.email",
        "https://www.googleapis.com/auth/youtube",
        "https://www.googleapis.com/auth/youtube.force-ssl",
        "https://www.googleapis.com/auth/youtube.readonly",
        "https://www.googleapis.com/auth/youtube.upload",
        "https://www.googleapis.com/auth/youtubepartner",
        "https://www.googleapis.com/auth/yt-analytics.readonly"
    };
    public override int MaxConcurrentJobs => 200;

    public YouTubeProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<YouTubeProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 5000;

    protected override ErrorHandlingResult? HandleErrors(string responseBody)
    {
        if (responseBody.Contains("invalidTitle"))
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "We have uploaded your video but we could not set the title. Title is too long.");

        if (responseBody.Contains("failedPrecondition"))
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "We have uploaded your video but we could not set the thumbnail. Thumbnail size is too large.");

        if (responseBody.Contains("uploadLimitExceeded"))
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "You have reached your daily upload limit, please try again tomorrow.");

        if (responseBody.Contains("youtubeSignupRequired"))
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "You have to link your YouTube account to your Google account first.");

        if (responseBody.Contains("youtube.thumbnail"))
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "Your account is not verified, we have uploaded your video but we could not set the thumbnail. Please verify your account and try again.");

        if (responseBody.Contains("Unauthorized"))
            return new ErrorHandlingResult(ErrorHandlingType.RefreshToken, "Token expired or invalid, please reconnect your YouTube account.");

        if (responseBody.Contains("UNAUTHENTICATED") || responseBody.Contains("invalid_grant"))
            return new ErrorHandlingResult(ErrorHandlingType.RefreshToken, "Please re-authenticate your YouTube account");

        return null;
    }

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(7);
        var clientId = GetClientId();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = $"{frontendUrl}/integrations/social/youtube";

        var url = $"https://accounts.google.com/o/oauth2/v2/auth" +
                  $"?client_id={clientId}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&response_type=code" +
                  $"&scope={Uri.EscapeDataString(string.Join(" ", Scopes))}" +
                  $"&access_type=offline" +
                  $"&prompt=consent" +
                  $"&state={state}";

        return new GenerateAuthUrlResponse
        {
            Url = url,
            CodeVerifier = MakeId(11),
            State = state
        };
    }

    public override async Task<AuthTokenDetails> AuthenticateAsync(
        AuthenticateParams parameters,
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetClientId();
        var clientSecret = GetClientSecret();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = $"{frontendUrl}/integrations/social/youtube";

        var tokenResponse = await ExchangeCodeForTokenAsync(clientId, clientSecret, redirectUri, parameters.Code, cancellationToken);

        var scopes = tokenResponse.Scope?.Split(' ') ?? Array.Empty<string>();
        CheckScopes(Scopes, string.Join(" ", scopes));

        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.Name,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken ?? string.Empty,
            ExpiresIn = tokenResponse.ExpiresIn ?? 3600,
            Picture = userInfo.Picture ?? string.Empty,
            Username = string.Empty
        };
    }

    public override async Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetClientId();
        var clientSecret = GetClientSecret();

        var tokenResponse = await RefreshAccessTokenAsync(clientId, clientSecret, refreshToken, cancellationToken);
        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.Name,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken ?? refreshToken,
            ExpiresIn = tokenResponse.ExpiresIn ?? 3600,
            Picture = userInfo.Picture ?? string.Empty,
            Username = string.Empty
        };
    }

    public override async Task<PostResponse[]> PostAsync(
        string id,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken = default)
    {
        var firstPost = posts.First();
        var settings = firstPost.Settings ?? new Dictionary<string, object>();

        if (firstPost.Media == null || firstPost.Media.Count == 0)
        {
            throw new ArgumentException("YouTube requires a video attachment");
        }

        var video = firstPost.Media[0];
        var title = GetSettingValue<string>(settings, "title") ?? "Untitled";
        var description = firstPost.Message;
        var tags = GetSettingValue<List<string>>(settings, "tags") ?? new List<string>();
        var categoryId = GetSettingValue<string>(settings, "category_id") ?? "22";
        var privacyStatus = GetSettingValue<string>(settings, "privacy_status") ?? "public";
        var madeForKids = GetSettingValue<bool?>(settings, "made_for_kids") ?? false;
        var thumbnail = GetSettingValue<string>(settings, "thumbnail_url");

        var videoId = await UploadVideoAsync(accessToken, video.Path, title, description, tags, categoryId, privacyStatus, madeForKids, cancellationToken);

        if (!string.IsNullOrEmpty(thumbnail))
        {
            await SetThumbnailAsync(accessToken, videoId, thumbnail, cancellationToken);
        }

        var videoUrl = $"https://www.youtube.com/watch?v={videoId}";

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = videoId,
                ReleaseUrl = videoUrl,
                Status = "success"
            }
        };
    }

    private async Task<string> UploadVideoAsync(
        string accessToken,
        string videoUrl,
        string title,
        string description,
        List<string> tags,
        string categoryId,
        string privacyStatus,
        bool madeForKids,
        CancellationToken cancellationToken)
    {
        var videoBytes = await ReadOrFetchAsync(videoUrl, cancellationToken);

        var metadata = new
        {
            snippet = new
            {
                title,
                description,
                tags = tags.Count > 0 ? tags.ToArray() : null,
                categoryId
            },
            status = new
            {
                privacyStatus,
                selfDeclaredMadeForKids = madeForKids
            }
        };

        var metadataJson = JsonSerializer.Serialize(metadata);

        using var videoStream = new MemoryStream(videoBytes);

        var request = new HttpRequestMessage(HttpMethod.Post, "https://www.googleapis.com/upload/youtube/v3/videos?uploadType=resumable&part=snippet,status");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Content = new StringContent(metadataJson, Encoding.UTF8, "application/json");

        var initResponse = await HttpClient.SendAsync(request, cancellationToken);
        initResponse.EnsureSuccessStatusCode();

        var uploadUrl = initResponse.Headers.Location?.ToString() ?? throw new InvalidOperationException("Failed to get upload URL");

        var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
        {
            Content = new ByteArrayContent(videoBytes)
        };
        uploadRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
        uploadRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/*");

        var uploadResponse = await HttpClient.SendAsync(uploadRequest, cancellationToken);
        uploadResponse.EnsureSuccessStatusCode();

        var videoResponse = await DeserializeAsync<YouTubeVideoResponse>(uploadResponse);
        return videoResponse.Id;
    }

    private async Task SetThumbnailAsync(string accessToken, string videoId, string thumbnailUrl, CancellationToken cancellationToken)
    {
        var thumbnailBytes = await ReadOrFetchAsync(thumbnailUrl, cancellationToken);

        var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(thumbnailBytes), "image", "thumbnail.jpg" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"https://www.googleapis.com/upload/youtube/v3/thumbnails/set?videoId={videoId}")
        {
            Content = content
        };
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        await HttpClient.SendAsync(request, cancellationToken);
    }

    public async Task<List<YouTubeChannel>> GetChannelsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/youtube/v3/channels?part=snippet,contentDetails,statistics&mine=true");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var channelsResponse = await DeserializeAsync<YouTubeChannelsResponse>(response);

        return channelsResponse.Items?.Select(c => new YouTubeChannel
        {
            Id = c.Id,
            Name = c.Snippet?.Title ?? "Unnamed Channel",
            Username = c.Snippet?.CustomUrl ?? string.Empty,
            Picture = c.Snippet?.Thumbnails?.Default?.Url ?? string.Empty,
            SubscriberCount = c.Statistics?.SubscriberCount ?? "0"
        }).ToList() ?? new List<YouTubeChannel>();
    }

    private async Task<GoogleTokenResponse> ExchangeCodeForTokenAsync(string clientId, string clientSecret, string redirectUri, string code, CancellationToken cancellationToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = content
        };

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<GoogleTokenResponse>(response);
    }

    private async Task<GoogleTokenResponse> RefreshAccessTokenAsync(string clientId, string clientSecret, string refreshToken, CancellationToken cancellationToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = content
        };

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<GoogleTokenResponse>(response);
    }

    private async Task<GoogleUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<GoogleUserInfo>(response);
    }

    private static T? GetSettingValue<T>(Dictionary<string, object> settings, string key)
    {
        if (!settings.TryGetValue(key, out var value))
            return default;

        if (value is T typedValue)
            return typedValue;

        var json = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<T>(json);
    }

    private string GetClientId() => _configuration["YOUTUBE_CLIENT_ID"] ?? throw new InvalidOperationException("YOUTUBE_CLIENT_ID not configured");
    private string GetClientSecret() => _configuration["YOUTUBE_CLIENT_SECRET"] ?? throw new InvalidOperationException("YOUTUBE_CLIENT_SECRET not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    private class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }

    private class GoogleUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }

    private class YouTubeVideoResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class YouTubeChannelsResponse
    {
        [JsonPropertyName("items")]
        public List<YouTubeChannelItem>? Items { get; set; }
    }

    private class YouTubeChannelItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("snippet")]
        public YouTubeSnippet? Snippet { get; set; }

        [JsonPropertyName("statistics")]
        public YouTubeStatistics? Statistics { get; set; }
    }

    private class YouTubeSnippet
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("customUrl")]
        public string? CustomUrl { get; set; }

        [JsonPropertyName("thumbnails")]
        public YouTubeThumbnails? Thumbnails { get; set; }
    }

    private class YouTubeThumbnails
    {
        [JsonPropertyName("default")]
        public YouTubeThumbnail? Default { get; set; }
    }

    private class YouTubeThumbnail
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    private class YouTubeStatistics
    {
        [JsonPropertyName("subscriberCount")]
        public string? SubscriberCount { get; set; }
    }

    public class YouTubeChannel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
        public string SubscriberCount { get; set; } = string.Empty;
    }

    #endregion
}
