using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class SlackProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "slack";
    public override string Name => "Slack";
    public override string[] Scopes => new[]
    {
        "channels:read",
        "chat:write",
        "users:read",
        "groups:read",
        "channels:join",
        "chat:write.customize"
    };
    public override int MaxConcurrentJobs => 3;

    public SlackProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SlackProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 400000;

    public override Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AuthTokenDetails
        {
            RefreshToken = string.Empty,
            ExpiresIn = 1000000,
            AccessToken = string.Empty,
            Id = string.Empty,
            Name = string.Empty,
            Picture = string.Empty,
            Username = string.Empty
        });
    }

    public override Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(6);
        var clientId = GetClientId();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = $"{frontendUrl}/integrations/social/slack";

        var url = $"https://slack.com/oauth/v2/authorize" +
                  $"?client_id={clientId}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&scope={string.Join(",", Scopes)}" +
                  $"&state={state}";

        return Task.FromResult(new GenerateAuthUrlResponse
        {
            Url = url,
            CodeVerifier = MakeId(10),
            State = state
        });
    }

    public override async Task<AuthTokenDetails> AuthenticateAsync(
        AuthenticateParams parameters,
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetClientId();
        var clientSecret = GetClientSecret();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = $"{frontendUrl}/integrations/social/slack";

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = parameters.Code,
            ["redirect_uri"] = redirectUri
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/oauth.v2.access")
        {
            Content = content
        };

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await DeserializeAsync<SlackTokenResponse>(response);
        
        CheckScopes(Scopes, string.Join(",", tokenResponse.Scope.Split(' ', ',')));

        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, tokenResponse.BotUserId, cancellationToken);

        return new AuthTokenDetails
        {
            Id = tokenResponse.Team.Id,
            Name = userInfo.RealName,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = "null",
            ExpiresIn = (int)TimeSpan.FromDays(100 * 365).TotalSeconds,
            Picture = userInfo.Profile?.ImageOriginal ?? string.Empty,
            Username = userInfo.Name
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
        var channel = firstPost.Settings?.GetValueOrDefault("channel")?.ToString()
            ?? throw new ArgumentException("Channel is required");

        await JoinChannelAsync(channel, accessToken, cancellationToken);

        var blocks = new List<object>
        {
            new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = firstPost.Message
                }
            }
        };

        if (firstPost.Media != null)
        {
            foreach (var media in firstPost.Media)
            {
                blocks.Add(new
                {
                    type = "image",
                    image_url = media.Path,
                    alt_text = media.Alt ?? string.Empty
                });
            }
        }

        var payload = new
        {
            channel,
            username = integration.Name,
            icon_url = integration.Picture,
            blocks
        };

        var request = CreateRequest("https://slack.com/api/chat.postMessage", HttpMethod.Post, payload);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var postResponse = await DeserializeAsync<SlackPostResponse>(response);

        var permalink = await GetPermalinkAsync(accessToken, postResponse.Channel, postResponse.Timestamp, cancellationToken);

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = postResponse.Timestamp,
                ReleaseUrl = permalink ?? string.Empty,
                Status = "posted"
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
        var commentPost = posts.First();
        var channel = commentPost.Settings?.GetValueOrDefault("channel")?.ToString()
            ?? throw new ArgumentException("Channel is required");
        var threadTs = lastCommentId ?? postId;

        var blocks = new List<object>
        {
            new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = commentPost.Message
                }
            }
        };

        if (commentPost.Media != null)
        {
            foreach (var media in commentPost.Media)
            {
                blocks.Add(new
                {
                    type = "image",
                    image_url = media.Path,
                    alt_text = media.Alt ?? string.Empty
                });
            }
        }

        var payload = new
        {
            channel,
            thread_ts = threadTs,
            username = integration.Name,
            icon_url = integration.Picture,
            blocks
        };

        var request = CreateRequest("https://slack.com/api/chat.postMessage", HttpMethod.Post, payload);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var postResponse = await DeserializeAsync<SlackPostResponse>(response);

        var permalink = await GetPermalinkAsync(accessToken, postResponse.Channel, postResponse.Timestamp, cancellationToken);

        return new[]
        {
            new PostResponse
            {
                Id = commentPost.Id,
                PostId = postResponse.Timestamp,
                ReleaseUrl = permalink ?? string.Empty,
                Status = "posted"
            }
        };
    }

    public async Task<List<SlackChannel>> GetChannelsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, 
            "https://slack.com/api/conversations.list?types=public_channel,private_channel");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var channelsResponse = await DeserializeAsync<SlackChannelsResponse>(response);
        return channelsResponse.Channels ?? new List<SlackChannel>();
    }

    private async Task JoinChannelAsync(string channel, string accessToken, CancellationToken cancellationToken)
    {
        var payload = new { channel };
        var request = CreateRequest("https://slack.com/api/conversations.join", HttpMethod.Post, payload);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        await HttpClient.SendAsync(request, cancellationToken);
    }

    private async Task<string?> GetPermalinkAsync(string accessToken, string channel, string messageTs, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://slack.com/api/chat.getPermalink?channel={channel}&message_ts={messageTs}");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var permalinkResponse = await DeserializeAsync<SlackPermalinkResponse>(response);
        return permalinkResponse.Permalink;
    }

    private async Task<SlackUser> GetUserInfoAsync(string accessToken, string botUserId, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://slack.com/api/users.info?user={botUserId}");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var userResponse = await DeserializeAsync<SlackUserResponse>(response);
        return userResponse.User;
    }

    private string GetClientId() => _configuration["SLACK_ID"] ?? throw new InvalidOperationException("SLACK_ID not configured");
    private string GetClientSecret() => _configuration["SLACK_SECRET"] ?? throw new InvalidOperationException("SLACK_SECRET not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    private class SlackTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;

        [JsonPropertyName("team")]
        public SlackTeam Team { get; set; } = new();

        [JsonPropertyName("bot_user_id")]
        public string BotUserId { get; set; } = string.Empty;
    }

    private class SlackTeam
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class SlackUserResponse
    {
        [JsonPropertyName("user")]
        public SlackUser User { get; set; } = new();
    }

    private class SlackUser
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("real_name")]
        public string RealName { get; set; } = string.Empty;

        [JsonPropertyName("profile")]
        public SlackProfile? Profile { get; set; }
    }

    private class SlackProfile
    {
        [JsonPropertyName("image_original")]
        public string? ImageOriginal { get; set; }
    }

    private class SlackPostResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("ts")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("channel")]
        public string Channel { get; set; } = string.Empty;
    }

    private class SlackPermalinkResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("permalink")]
        public string? Permalink { get; set; }
    }

    private class SlackChannelsResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("channels")]
        public List<SlackChannel>? Channels { get; set; }
    }

    public class SlackChannel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
