using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class TwitchProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "twitch";
    public override string Name => "Twitch";
    public override string[] Scopes => new[] { "user:write:chat", "user:read:chat", "moderator:manage:announcements" };
    public override int MaxConcurrentJobs => 1;

    public TwitchProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TwitchProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 500;

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(32);
        var clientId = GetClientId();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = $"{frontendUrl}/integrations/social/twitch";

        var url = "https://id.twitch.tv/oauth2/authorize" +
                  $"?response_type=code" +
                  $"&client_id={clientId}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&scope={Uri.EscapeDataString(string.Join(" ", Scopes))}" +
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
        var clientId = GetClientId();
        var clientSecret = GetClientSecret();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = $"{frontendUrl}/integrations/social/twitch{(parameters.Refresh != null ? $"?refresh={parameters.Refresh}" : "")}";

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["code"] = parameters.Code
        };

        var response = await HttpClient.PostAsync("https://id.twitch.tv/oauth2/token", new FormUrlEncodedContent(form), cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenInfo = await DeserializeAsync<TwitchTokenResponse>(response);
        var userInfo = await GetUserInfoAsync(tokenInfo.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.Name,
            AccessToken = tokenInfo.AccessToken,
            RefreshToken = tokenInfo.RefreshToken,
            ExpiresIn = tokenInfo.ExpiresIn,
            Picture = userInfo.Picture ?? string.Empty,
            Username = userInfo.Username
        };
    }

    public override async Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetClientId();
        var clientSecret = GetClientSecret();

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["refresh_token"] = refreshToken
        };

        var response = await HttpClient.PostAsync("https://id.twitch.tv/oauth2/token", new FormUrlEncodedContent(form), cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenInfo = await DeserializeAsync<TwitchTokenResponse>(response);
        var userInfo = await GetUserInfoAsync(tokenInfo.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.Name,
            AccessToken = tokenInfo.AccessToken,
            RefreshToken = tokenInfo.RefreshToken,
            ExpiresIn = tokenInfo.ExpiresIn,
            Picture = userInfo.Picture ?? string.Empty,
            Username = userInfo.Username
        };
    }

    public override async Task<PostResponse[]> PostAsync(
        string id,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(2000, cancellationToken);

        if (posts.Count == 0)
            return Array.Empty<PostResponse>();

        var firstPost = posts[0];
        var settings = firstPost.Settings ?? new Dictionary<string, object>();

        var messageType = GetSettingValue<string>(settings, "messageType") ?? "message";
        var announcementColor = GetSettingValue<string>(settings, "announcementColor") ?? "primary";

        if (messageType == "announcement")
        {
            await SendAnnouncementAsync(id, accessToken, firstPost.Message, announcementColor, cancellationToken);

            return new[]
            {
                new PostResponse
                {
                    Id = firstPost.Id,
                    PostId = MakeId(10),
                    ReleaseUrl = $"https://twitch.tv/{integration.Username}",
                    Status = "posted"
                }
            };
        }

        var result = await SendChatMessageAsync(id, accessToken, firstPost.Message, null, cancellationToken);

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = result.MessageId,
                ReleaseUrl = $"https://twitch.tv/{integration.Username}",
                Status = result.IsSent ? "posted" : "error"
            }
        };
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
        await Task.Delay(2000, cancellationToken);

        if (posts.Count == 0)
            return Array.Empty<PostResponse>();

        var commentPost = posts[0];
        var settings = commentPost.Settings ?? new Dictionary<string, object>();

        var messageType = GetSettingValue<string>(settings, "messageType") ?? "message";
        var announcementColor = GetSettingValue<string>(settings, "announcementColor") ?? "primary";

        if (messageType == "announcement")
        {
            await SendAnnouncementAsync(id, accessToken, commentPost.Message, announcementColor, cancellationToken);

            return new[]
            {
                new PostResponse
                {
                    Id = commentPost.Id,
                    PostId = MakeId(10),
                    ReleaseUrl = $"https://twitch.tv/{integration.Username}",
                    Status = "posted"
                }
            };
        }

        var result = await SendChatMessageAsync(id, accessToken, commentPost.Message, lastCommentId ?? postId, cancellationToken);

        return new[]
        {
            new PostResponse
            {
                Id = commentPost.Id,
                PostId = result.MessageId,
                ReleaseUrl = $"https://twitch.tv/{integration.Username}",
                Status = result.IsSent ? "posted" : "error"
            }
        };
    }

    private async Task<TwitchUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var clientId = GetClientId();

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/users");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        request.Headers.TryAddWithoutValidation("Client-Id", clientId);

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var userResponse = await DeserializeAsync<TwitchUserResponse>(response);
        var user = userResponse.Data?.FirstOrDefault() ?? throw new BadBodyException(Identifier, "User not found");

        return new TwitchUserInfo
        {
            Id = user.Id,
            Name = user.DisplayName ?? "",
            Username = user.Login ?? "",
            Picture = user.ProfileImageUrl
        };
    }

    private async Task SendAnnouncementAsync(
        string broadcasterId,
        string accessToken,
        string message,
        string color,
        CancellationToken cancellationToken)
    {
        var clientId = GetClientId();

        var payload = new
        {
            message = message.Substring(0, Math.Min(message.Length, 500)),
            color
        };

        var url = $"https://api.twitch.tv/helix/chat/announcements?broadcaster_id={broadcasterId}&moderator_id={broadcasterId}";

        var request = CreateJsonRequest(url, HttpMethod.Post, payload, accessToken);
        request.Headers.TryAddWithoutValidation("Client-Id", clientId);

        await HttpClient.SendAsync(request, cancellationToken);
    }

    private async Task<TwitchChatResult> SendChatMessageAsync(
        string broadcasterId,
        string accessToken,
        string message,
        string? replyToMessageId,
        CancellationToken cancellationToken)
    {
        var clientId = GetClientId();

        var payload = new Dictionary<string, object?>
        {
            ["broadcaster_id"] = broadcasterId,
            ["sender_id"] = broadcasterId,
            ["message"] = message.Substring(0, Math.Min(message.Length, 500))
        };

        if (!string.IsNullOrEmpty(replyToMessageId))
        {
            payload["reply_parent_message_id"] = replyToMessageId;
        }

        var request = CreateJsonRequest("https://api.twitch.tv/helix/chat/messages", HttpMethod.Post, payload, accessToken);
        request.Headers.TryAddWithoutValidation("Client-Id", clientId);

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var chatResponse = await DeserializeAsync<TwitchChatResponse>(response);
        var chatData = chatResponse.Data?.FirstOrDefault();

        return new TwitchChatResult
        {
            MessageId = chatData?.MessageId ?? MakeId(10),
            IsSent = chatData?.IsSent ?? false
        };
    }

    private static HttpRequestMessage CreateJsonRequest(string url, HttpMethod method, object? payload, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);

        if (payload != null)
        {
            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        return request;
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

    private string GetClientId() => _configuration["TWITCH_CLIENT_ID"] ?? throw new InvalidOperationException("TWITCH_CLIENT_ID not configured");
    private string GetClientSecret() => _configuration["TWITCH_CLIENT_SECRET"] ?? throw new InvalidOperationException("TWITCH_CLIENT_SECRET not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    private class TwitchTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public List<string>? Scope { get; set; }
    }

    private class TwitchUserResponse
    {
        [JsonPropertyName("data")]
        public List<TwitchUser>? Data { get; set; }
    }

    private class TwitchUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("login")]
        public string? Login { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("profile_image_url")]
        public string? ProfileImageUrl { get; set; }
    }

    private class TwitchUserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Picture { get; set; }
    }

    private class TwitchChatResponse
    {
        [JsonPropertyName("data")]
        public List<TwitchChatData>? Data { get; set; }
    }

    private class TwitchChatData
    {
        [JsonPropertyName("message_id")]
        public string? MessageId { get; set; }

        [JsonPropertyName("is_sent")]
        public bool IsSent { get; set; }
    }

    private class TwitchChatResult
    {
        public string MessageId { get; set; } = string.Empty;
        public bool IsSent { get; set; }
    }

    #endregion
}
