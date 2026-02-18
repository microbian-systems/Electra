using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class DevToProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "devto";
    public override string Name => "Dev.to";
    public override string[] Scopes => Array.Empty<string>();
    public override int MaxConcurrentJobs => 3;
    public override EditorType Editor => EditorType.Markdown;

    public DevToProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<DevToProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 100000;

    protected override ErrorHandlingResult? HandleErrors(string responseBody)
    {
        if (responseBody.Contains("Canonical url has already been taken"))
        {
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "Canonical URL already exists");
        }

        return null;
    }

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(6);
        return new GenerateAuthUrlResponse
        {
            Url = "",
            CodeVerifier = MakeId(10),
            State = state
        };
    }

    public override async Task<AuthTokenDetails> AuthenticateAsync(
        AuthenticateParams parameters,
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var bodyBytes = Convert.FromBase64String(parameters.Code);
        var bodyJson = Encoding.UTF8.GetString(bodyBytes);
        var authBody = JsonSerializer.Deserialize<DevToAuthBody>(bodyJson)
            ?? throw new BadBodyException(Identifier, "Invalid auth body");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://dev.to/api/users/me");
            request.Headers.TryAddWithoutValidation("api-key", authBody.ApiKey);

            var response = await HttpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new BadBodyException(Identifier, "Invalid credentials");
            }

            var userInfo = await DeserializeAsync<DevToUserInfo>(response);

            return new AuthTokenDetails
            {
                RefreshToken = "",
                ExpiresIn = (int)TimeSpan.FromDays(100).TotalSeconds,
                AccessToken = authBody.ApiKey,
                Id = userInfo.Id.ToString(),
                Name = userInfo.Name ?? "",
                Picture = userInfo.ProfileImage ?? string.Empty,
                Username = userInfo.Username ?? ""
            };
        }
        catch (Exception)
        {
            throw new BadBodyException(Identifier, "Invalid credentials");
        }
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
        if (posts.Count == 0)
            return Array.Empty<PostResponse>();

        var firstPost = posts[0];
        var settings = firstPost.Settings ?? new Dictionary<string, object>();

        var title = GetSettingValue<string>(settings, "title") ?? "";
        var mainImage = GetSettingValue<MediaContent>(settings, "main_image");
        var tags = GetSettingValue<List<DevToTag>>(settings, "tags") ?? new List<DevToTag>();
        var organization = GetSettingValue<string>(settings, "organization");
        var canonical = GetSettingValue<string>(settings, "canonical");

        var article = new Dictionary<string, object?>
        {
            ["title"] = title,
            ["body_markdown"] = firstPost.Message,
            ["published"] = true
        };

        if (mainImage?.Path != null)
        {
            article["main_image"] = mainImage.Path;
        }

        if (tags.Count > 0)
        {
            article["tags"] = tags.Select(t => t.Label).ToArray();
        }

        if (!string.IsNullOrEmpty(organization))
        {
            article["organization_id"] = organization;
        }

        if (!string.IsNullOrEmpty(canonical))
        {
            article["canonical_url"] = canonical;
        }

        var payload = new { article };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://dev.to/api/articles") { Content = content };
        request.Headers.TryAddWithoutValidation("api-key", accessToken);

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var articleResponse = await DeserializeAsync<DevToArticleResponse>(response);

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                Status = "completed",
                PostId = articleResponse.Id.ToString(),
                ReleaseUrl = articleResponse.Url ?? ""
            }
        };
    }

    public async Task<List<DevToTag>> GetTagsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://dev.to/api/tags?per_page=1000&page=1");
        request.Headers.TryAddWithoutValidation("api-key", accessToken);

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tags = await DeserializeAsync<List<DevToTagResponse>>(response);
        return tags?.Select(t => new DevToTag { Value = t.Id, Label = t.Name }).ToList() ?? new List<DevToTag>();
    }

    public async Task<List<DevToOrganization>> GetOrganizationsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://dev.to/api/articles/me/all?per_page=1000");
        request.Headers.TryAddWithoutValidation("api-key", accessToken);

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var articles = await DeserializeAsync<List<DevToArticleItem>>(response);

        var orgUsernames = articles
            ?.Where(a => a.Organization?.Username != null)
            .Select(a => a.Organization!.Username!)
            .Distinct()
            .ToList() ?? new List<string>();

        var organizations = new List<DevToOrganization>();

        foreach (var orgUsername in orgUsernames)
        {
            var orgRequest = new HttpRequestMessage(HttpMethod.Get, $"https://dev.to/api/organizations/{orgUsername}");
            orgRequest.Headers.TryAddWithoutValidation("api-key", accessToken);

            var orgResponse = await HttpClient.SendAsync(orgRequest, cancellationToken);
            if (orgResponse.IsSuccessStatusCode)
            {
                var orgInfo = await DeserializeAsync<DevToOrganizationResponse>(orgResponse);
                if (orgInfo != null)
                {
                    organizations.Add(new DevToOrganization
                    {
                        Id = orgInfo.Id.ToString(),
                        Name = orgInfo.Name ?? "",
                        Username = orgInfo.Username ?? ""
                    });
                }
            }
        }

        return organizations;
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

    #region DTOs

    private class DevToAuthBody
    {
        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = string.Empty;
    }

    private class DevToUserInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("profile_image")]
        public string? ProfileImage { get; set; }
    }

    private class DevToArticleResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    private class DevToTagResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private class DevToArticleItem
    {
        [JsonPropertyName("organization")]
        public DevToOrganizationRef? Organization { get; set; }
    }

    private class DevToOrganizationRef
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }

    private class DevToOrganizationResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }

    public class DevToTag
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class DevToOrganization
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }

    #endregion
}
