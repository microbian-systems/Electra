using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class MastodonProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "mastodon";
    public override string Name => "Mastodon";
    public override string[] Scopes => new[] { "write:statuses", "profile", "write:media" };
    public override int MaxConcurrentJobs => 5;

    public MastodonProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MastodonProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 500;

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(6);
        var instanceUrl = GetInstanceUrl();
        var clientId = GetClientId();
        var frontendUrl = GetFrontendUrl();

        var url = $"{instanceUrl}/oauth/authorize" +
                  $"?client_id={clientId}" +
                  $"&response_type=code" +
                  $"&redirect_uri={Uri.EscapeDataString($"{frontendUrl}/integrations/social/mastodon")}" +
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
        var instanceUrl = GetInstanceUrl();
        var clientId = GetClientId();
        var clientSecret = GetClientSecret();
        var frontendUrl = GetFrontendUrl();

        var form = new MultipartFormDataContent();
        form.Add(new StringContent(clientId), "client_id");
        form.Add(new StringContent(clientSecret), "client_secret");
        form.Add(new StringContent(parameters.Code), "code");
        form.Add(new StringContent("authorization_code"), "grant_type");
        form.Add(new StringContent($"{frontendUrl}/integrations/social/mastodon"), "redirect_uri");
        form.Add(new StringContent(string.Join(" ", Scopes)), "scope");

        var tokenUrl = $"{instanceUrl}/oauth/token";
        var tokenResponse = await HttpClient.PostAsync(tokenUrl, form, cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenInfo = await DeserializeAsync<MastodonTokenResponse>(tokenResponse);
        var userInfo = await FetchUserInfoAsync(instanceUrl, tokenInfo.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.DisplayName ?? userInfo.Username,
            AccessToken = tokenInfo.AccessToken,
            RefreshToken = "null",
            ExpiresIn = (int)TimeSpan.FromDays(100).TotalSeconds,
            Picture = userInfo.Avatar ?? string.Empty,
            Username = userInfo.Username
        };
    }

    public override Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AuthTokenDetails
        {
            RefreshToken = string.Empty,
            ExpiresIn = 0,
            AccessToken = string.Empty,
            Id = string.Empty,
            Name = string.Empty,
            Picture = string.Empty,
            Username = string.Empty
        });
    }

    public override async Task<PostResponse[]> PostAsync(
        string id,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken = default)
    {
        var instanceUrl = GetInstanceUrl();

        if (posts.Count == 0)
            return Array.Empty<PostResponse>();

        var firstPost = posts[0];

        var uploadedMediaIds = new List<string>();
        if (firstPost.Media != null)
        {
            foreach (var media in firstPost.Media)
            {
                var mediaId = await UploadMediaAsync(instanceUrl, media.Path, accessToken, cancellationToken);
                uploadedMediaIds.Add(mediaId);
            }
        }

        var form = new MultipartFormDataContent();
        form.Add(new StringContent(firstPost.Message), "status");
        form.Add(new StringContent("public"), "visibility");

        foreach (var mediaId in uploadedMediaIds)
        {
            form.Add(new StringContent(mediaId), "media_ids[]");
        }

        var statusUrl = $"{instanceUrl}/api/v1/statuses";
        var request = new HttpRequestMessage(HttpMethod.Post, statusUrl)
        {
            Content = form
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var statusInfo = await DeserializeAsync<MastodonStatusResponse>(response);

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = statusInfo.Id,
                ReleaseUrl = $"{instanceUrl}/statuses/{statusInfo.Id}",
                Status = "completed"
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
        var instanceUrl = GetInstanceUrl();

        if (posts.Count == 0)
            return Array.Empty<PostResponse>();

        var commentPost = posts[0];
        var replyToId = lastCommentId ?? postId;

        var uploadedMediaIds = new List<string>();
        if (commentPost.Media != null)
        {
            foreach (var media in commentPost.Media)
            {
                var mediaId = await UploadMediaAsync(instanceUrl, media.Path, accessToken, cancellationToken);
                uploadedMediaIds.Add(mediaId);
            }
        }

        var form = new MultipartFormDataContent();
        form.Add(new StringContent(commentPost.Message), "status");
        form.Add(new StringContent("public"), "visibility");
        form.Add(new StringContent(replyToId), "in_reply_to_id");

        foreach (var mediaId in uploadedMediaIds)
        {
            form.Add(new StringContent(mediaId), "media_ids[]");
        }

        var statusUrl = $"{instanceUrl}/api/v1/statuses";
        var request = new HttpRequestMessage(HttpMethod.Post, statusUrl)
        {
            Content = form
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var statusInfo = await DeserializeAsync<MastodonStatusResponse>(response);

        return new[]
        {
            new PostResponse
            {
                Id = commentPost.Id,
                PostId = statusInfo.Id,
                ReleaseUrl = $"{instanceUrl}/statuses/{statusInfo.Id}",
                Status = "completed"
            }
        };
    }

    private async Task<string> UploadMediaAsync(
        string instanceUrl,
        string mediaUrl,
        string accessToken,
        CancellationToken cancellationToken)
    {
        byte[] mediaBytes;
        if (mediaUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            mediaBytes = await HttpClient.GetByteArrayAsync(mediaUrl, cancellationToken);
        }
        else
        {
            mediaBytes = await File.ReadAllBytesAsync(mediaUrl, cancellationToken);
        }

        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(mediaBytes), "file", Path.GetFileName(mediaUrl) ?? "media");

        var uploadUrl = $"{instanceUrl}/api/v1/media";
        var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
        {
            Content = form
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var mediaInfo = await DeserializeAsync<MastodonMediaResponse>(response);
        return mediaInfo.Id;
    }

    private async Task<MastodonUserInfo> FetchUserInfoAsync(
        string instanceUrl,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var url = $"{instanceUrl}/api/v1/accounts/verify_credentials";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<MastodonUserInfo>(response);
    }

    private string GetInstanceUrl() => _configuration["MASTODON_URL"] ?? "https://mastodon.social";
    private string GetClientId() => _configuration["MASTODON_CLIENT_ID"] ?? throw new InvalidOperationException("MASTODON_CLIENT_ID not configured");
    private string GetClientSecret() => _configuration["MASTODON_CLIENT_SECRET"] ?? throw new InvalidOperationException("MASTODON_CLIENT_SECRET not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    private class MastodonTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("created_at")]
        public long? CreatedAt { get; set; }
    }

    private class MastodonUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }
    }

    private class MastodonStatusResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    private class MastodonMediaResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    #endregion
}
