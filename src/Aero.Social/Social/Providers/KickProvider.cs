using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class KickProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "kick";
    public override string Name => "Kick";
    public override string[] Scopes => new[] { "chat:write", "user:read", "channel:read" };
    public override int MaxConcurrentJobs => 3;

    public KickProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<KickProvider> logger)
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
        var (codeVerifier, codeChallenge) = GeneratePKCE();

        var clientId = GetClientId();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = $"{frontendUrl}/integrations/social/kick";

        var url = "https://id.kick.com/oauth/authorize" +
                  $"?response_type=code" +
                  $"&client_id={clientId}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&scope={Uri.EscapeDataString(string.Join(" ", Scopes))}" +
                  $"&state={state}" +
                  $"&code_challenge={codeChallenge}" +
                  $"&code_challenge_method=S256";

        return new GenerateAuthUrlResponse
        {
            Url = url,
            CodeVerifier = codeVerifier,
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
        var redirectUri = $"{frontendUrl}/integrations/social/kick{(parameters.Refresh != null ? $"?refresh={parameters.Refresh}" : "")}";

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["code"] = parameters.Code,
            ["code_verifier"] = parameters.CodeVerifier ?? ""
        };

        var response = await HttpClient.PostAsync("https://id.kick.com/oauth/token", new FormUrlEncodedContent(form), cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenInfo = await DeserializeAsync<KickTokenResponse>(response);
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

        var response = await HttpClient.PostAsync("https://id.kick.com/oauth/token", new FormUrlEncodedContent(form), cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenInfo = await DeserializeAsync<KickTokenResponse>(response);
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
        if (posts.Count == 0)
            return Array.Empty<PostResponse>();

        var firstPost = posts[0];

        var payload = new
        {
            type = "user",
            content = firstPost.Message.Substring(0, Math.Min(firstPost.Message.Length, 500)),
            broadcaster_user_id = int.Parse(id)
        };

        var request = CreateJsonRequest("https://api.kick.com/public/v1/chat", HttpMethod.Post, payload, accessToken);

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var chatResponse = await DeserializeAsync<KickChatResponse>(response);

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = chatResponse.Data?.MessageId ?? MakeId(10),
                ReleaseUrl = $"https://kick.com/{integration.Username ?? "channel"}",
                Status = chatResponse.Data?.IsSent == true ? "posted" : "error"
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
        if (posts.Count == 0)
            return Array.Empty<PostResponse>();

        var commentPost = posts[0];

        var payload = new Dictionary<string, object?>
        {
            ["type"] = "user",
            ["content"] = commentPost.Message.Substring(0, Math.Min(commentPost.Message.Length, 500)),
            ["broadcaster_user_id"] = int.Parse(id),
            ["reply_to_message_id"] = lastCommentId ?? postId
        };

        var request = CreateJsonRequest("https://api.kick.com/public/v1/chat", HttpMethod.Post, payload, accessToken);

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var chatResponse = await DeserializeAsync<KickChatResponse>(response);

        return new[]
        {
            new PostResponse
            {
                Id = commentPost.Id,
                PostId = chatResponse.Data?.MessageId ?? MakeId(10),
                ReleaseUrl = $"https://kick.com/{integration.Username ?? "channel"}",
                Status = chatResponse.Data?.IsSent == true ? "posted" : "error"
            }
        };
    }

    private async Task<KickUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.kick.com/public/v1/users");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var userResponse = await DeserializeAsync<KickUserResponse>(response);
        var user = userResponse.Data?.FirstOrDefault() ?? throw new BadBodyException(Identifier, "User not found");

        return new KickUserInfo
        {
            Id = user.UserId ?? user.Id?.ToString() ?? "",
            Name = user.Name ?? "",
            Username = user.Name ?? "",
            Picture = user.ProfilePicture
        };
    }

    private static (string CodeVerifier, string CodeChallenge) GeneratePKCE()
    {
        var codeVerifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        using var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        var codeChallenge = Convert.ToBase64String(challengeBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return (codeVerifier, codeChallenge);
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

    private string GetClientId() => _configuration["KICK_CLIENT_ID"] ?? throw new InvalidOperationException("KICK_CLIENT_ID not configured");
    private string GetClientSecret() => _configuration["KICK_SECRET"] ?? throw new InvalidOperationException("KICK_SECRET not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    private class KickTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }

    private class KickUserResponse
    {
        [JsonPropertyName("data")]
        public List<KickUser>? Data { get; set; }
    }

    private class KickUser
    {
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("profile_picture")]
        public string? ProfilePicture { get; set; }
    }

    private class KickUserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Picture { get; set; }
    }

    private class KickChatResponse
    {
        [JsonPropertyName("data")]
        public KickChatData? Data { get; set; }
    }

    private class KickChatData
    {
        [JsonPropertyName("message_id")]
        public string? MessageId { get; set; }

        [JsonPropertyName("is_sent")]
        public bool IsSent { get; set; }
    }

    #endregion
}
