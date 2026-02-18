using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class DribbbleProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "dribbble";
    public override string Name => "Dribbble";
    public override string[] Scopes => new[] { "public", "upload" };
    public override int MaxConcurrentJobs => 3;

    public DribbbleProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<DribbbleProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 40000;

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(6);
        var clientId = GetClientId();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = $"{frontendUrl}/integrations/social/dribbble";

        var url = "https://dribbble.com/oauth/authorize" +
                  $"?client_id={clientId}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&response_type=code" +
                  $"&scope={string.Join("+", Scopes)}" +
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

        var tokenUrl = $"https://dribbble.com/oauth/token" +
                       $"?client_id={clientId}" +
                       $"&client_secret={clientSecret}" +
                       $"&code={parameters.Code}" +
                       $"&redirect_uri={Uri.EscapeDataString($"{frontendUrl}/integrations/social/dribbble")}";

        var tokenResponse = await HttpClient.PostAsync(tokenUrl, null, cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenInfo = await DeserializeAsync<DribbbleTokenResponse>(tokenResponse);
        CheckScopes(Scopes, tokenInfo.Scope ?? "");

        var userInfo = await GetUserInfoAsync(tokenInfo.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id.ToString(),
            Name = userInfo.Name ?? "",
            AccessToken = tokenInfo.AccessToken,
            RefreshToken = "",
            ExpiresIn = 999999999,
            Picture = userInfo.AvatarUrl ?? string.Empty,
            Username = userInfo.Login ?? ""
        };
    }

    public override Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AuthTokenDetails
        {
            RefreshToken = "",
            ExpiresIn = 0,
            AccessToken = "",
            Id = "",
            Name = "",
            Picture = "",
            Username = ""
        });
    }

    public override async Task<PostResponse[]> PostAsync(
        string id,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken = default)
    {
        if (posts.Count == 0 || posts[0].Media == null || posts[0].Media.Count == 0)
            return Array.Empty<PostResponse>();

        var firstPost = posts[0];
        var media = firstPost.Media[0];
        var settings = firstPost.Settings ?? new Dictionary<string, object>();

        var title = GetSettingValue<string>(settings, "title") ?? "";

        byte[] imageBytes;
        string fileName;

        if (media.Path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            imageBytes = await HttpClient.GetByteArrayAsync(media.Path, cancellationToken);
            fileName = media.Path.Split('/').Last() ?? "image.png";
        }
        else
        {
            imageBytes = await File.ReadAllBytesAsync(media.Path, cancellationToken);
            fileName = Path.GetFileName(media.Path) ?? "image.png";
        }

        var contentType = GetContentType(fileName);

        using var formData = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        formData.Add(imageContent, "image", fileName);
        formData.Add(new StringContent(title), "title");
        formData.Add(new StringContent(firstPost.Message), "description");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dribbble.com/v2/shots") { Content = formData };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location?.ToString() ?? "";
        var newId = location.Split('/').Last() ?? "";

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                Status = "completed",
                PostId = newId,
                ReleaseUrl = $"https://dribbble.com/shots/{newId}"
            }
        };
    }

    public async Task<List<DribbbleTeam>> GetTeamsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.dribbble.com/v2/user");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var userResponse = await DeserializeAsync<DribbbleUserWithTeams>(response);

        return userResponse.Teams?.Select(t => new DribbbleTeam
        {
            Id = t.Id.ToString(),
            Name = t.Name ?? ""
        }).ToList() ?? new List<DribbbleTeam>();
    }

    private async Task<DribbbleUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.dribbble.com/v2/user");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<DribbbleUserInfo>(response);
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
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

    private string GetClientId() => _configuration["DRIBBBLE_CLIENT_ID"] ?? throw new InvalidOperationException("DRIBBBLE_CLIENT_ID not configured");
    private string GetClientSecret() => _configuration["DRIBBBLE_CLIENT_SECRET"] ?? throw new InvalidOperationException("DRIBBBLE_CLIENT_SECRET not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    private class DribbbleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }

    private class DribbbleUserInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("login")]
        public string? Login { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }
    }

    private class DribbbleUserWithTeams : DribbbleUserInfo
    {
        [JsonPropertyName("teams")]
        public List<DribbbleTeamInfo>? Teams { get; set; }
    }

    private class DribbbleTeamInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class DribbbleTeam
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
