using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class LinkedInProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "linkedin";
    public override string Name => "LinkedIn";
    public override string[] Scopes => new[]
    {
        "openid",
        "profile",
        "w_member_social",
        "r_basicprofile",
        "rw_organization_admin",
        "w_organization_social",
        "r_organization_social"
    };
    public override bool OneTimeToken => true;
    public override bool RefreshWait => true;
    public override int MaxConcurrentJobs => 2;

    public LinkedInProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LinkedInProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 3000;

    protected override ErrorHandlingResult? HandleErrors(string responseBody)
    {
        if (responseBody.Contains("Unable to obtain activity"))
        {
            return new ErrorHandlingResult(ErrorHandlingType.Retry, "Unable to obtain activity");
        }
        return null;
    }

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(6);
        var codeVerifier = MakeId(30);
        var clientId = GetClientId();
        var frontendUrl = GetFrontendUrl();

        var url = $"https://www.linkedin.com/oauth/v2/authorization" +
                  $"?response_type=code" +
                  $"&client_id={clientId}" +
                  $"&prompt=none" +
                  $"&redirect_uri={Uri.EscapeDataString($"{frontendUrl}/integrations/social/linkedin")}" +
                  $"&state={state}" +
                  $"&scope={Uri.EscapeDataString(string.Join(" ", Scopes))}";

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
        var redirectUri = $"{frontendUrl}/integrations/social/linkedin";

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = parameters.Code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://www.linkedin.com/oauth/v2/accessToken")
        {
            Content = content
        };

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await DeserializeAsync<LinkedInTokenResponse>(response);
        CheckScopes(Scopes, tokenResponse.Scope);

        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, cancellationToken);
        var vanityName = await GetVanityNameAsync(tokenResponse.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Sub,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresIn = tokenResponse.ExpiresIn,
            Name = userInfo.Name,
            Picture = userInfo.Picture ?? string.Empty,
            Username = vanityName
        };
    }

    public override async Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetClientId();
        var clientSecret = GetClientSecret();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://www.linkedin.com/oauth/v2/accessToken")
        {
            Content = content
        };

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await DeserializeAsync<LinkedInTokenResponse>(response);
        var vanityName = await GetVanityNameAsync(tokenResponse.AccessToken, cancellationToken);
        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Sub,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresIn = tokenResponse.ExpiresIn,
            Name = userInfo.Name,
            Picture = userInfo.Picture ?? string.Empty,
            Username = vanityName
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

        var postAsCarousel = GetSettingValue<bool>(settings, "post_as_images_carousel");

        List<string> mediaIds = new();

        if (firstPost.Media != null && firstPost.Media.Count > 0)
        {
            foreach (var media in firstPost.Media)
            {
                var mediaId = await UploadMediaAsync(id, accessToken, media, cancellationToken);
                mediaIds.Add(mediaId);
            }
        }

        var author = $"urn:li:person:{id}";
        var message = FixText(firstPost.Message);

        var payload = new Dictionary<string, object>
        {
            ["author"] = author,
            ["commentary"] = message,
            ["visibility"] = "PUBLIC",
            ["distribution"] = new
            {
                feedDistribution = "MAIN_FEED",
                targetEntities = Array.Empty<string>(),
                thirdPartyDistributionChannels = Array.Empty<string>()
            },
            ["lifecycleState"] = "PUBLISHED",
            ["isReshareDisabledByAuthor"] = false
        };

        if (mediaIds.Count > 0)
        {
            if (mediaIds.Count == 1)
            {
                payload["content"] = new { media = new { id = mediaIds[0] } };
            }
            else
            {
                payload["content"] = new
                {
                    multiImage = new
                    {
                        images = mediaIds.Select(m => new { id = m }).ToArray()
                    }
                };
            }
        }

        var request = CreateRequest("https://api.linkedin.com/rest/posts", HttpMethod.Post, payload);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Headers.Add("LinkedIn-Version", "202511");
        request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

        var response = await HttpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new BadBodyException(Identifier, error);
        }

        var postId = response.Headers.GetValues("x-restli-id").FirstOrDefault() ?? string.Empty;

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = postId,
                ReleaseUrl = $"https://www.linkedin.com/feed/update/{postId}",
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
        var actor = $"urn:li:person:{id}";

        var payload = new
        {
            actor,
            @object = postId,
            message = new { text = FixText(commentPost.Message) }
        };

        var request = CreateRequest($"https://api.linkedin.com/v2/socialActions/{Uri.EscapeDataString(postId)}/comments", HttpMethod.Post, payload);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var commentResponse = await DeserializeAsync<LinkedInCommentResponse>(response);

        return new[]
        {
            new PostResponse
            {
                Id = commentPost.Id,
                PostId = commentResponse.Object,
                ReleaseUrl = $"https://www.linkedin.com/embed/feed/update/{commentResponse.Object}",
                Status = "posted"
            }
        };
    }

    private async Task<string> UploadMediaAsync(string personId, string accessToken, MediaContent media, CancellationToken cancellationToken)
    {
        var isVideo = media.Path.Contains(".mp4", StringComparison.OrdinalIgnoreCase);
        var endpoint = isVideo ? "videos" : "images";

        var mediaBytes = await ReadOrFetchAsync(media.Path, cancellationToken);

        var initializePayload = new
        {
            initializeUploadRequest = new
            {
                owner = $"urn:li:person:{personId}",
                fileSizeBytes = isVideo ? mediaBytes.Length : (int?)null
            }
        };

        var request = CreateRequest($"https://api.linkedin.com/rest/{endpoint}?action=initializeUpload", HttpMethod.Post, initializePayload);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Headers.Add("LinkedIn-Version", "202511");
        request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var uploadResponse = await DeserializeAsync<LinkedInUploadResponse>(response);

        var uploadUrl = uploadResponse.Value.UploadUrl;
        var imageId = uploadResponse.Value.Image;

        var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
        {
            Content = new ByteArrayContent(mediaBytes)
        };
        uploadRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
        uploadRequest.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
        uploadRequest.Headers.Add("LinkedIn-Version", "202511");

        if (isVideo)
        {
            uploadRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        }

        await HttpClient.SendAsync(uploadRequest, cancellationToken);

        return imageId;
    }

    protected async Task<LinkedInUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.linkedin.com/v2/userinfo");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<LinkedInUserInfo>(response);
    }

    protected async Task<string> GetVanityNameAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.linkedin.com/v2/me");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var meResponse = await DeserializeAsync<LinkedInMeResponse>(response);
        return meResponse.VanityName ?? string.Empty;
    }

    protected static string FixText(string text)
    {
        var specialChars = new[] { "\\", "<", ">", "#", "~", "_", "|", "[", "]", "*", "(", ")", "{", "}", "@" };
        foreach (var ch in specialChars)
        {
            text = text.Replace(ch, $"\\{ch}");
        }
        return text;
    }

    protected static T? GetSettingValue<T>(Dictionary<string, object> settings, string key)
    {
        if (!settings.TryGetValue(key, out var value))
            return default;

        if (value is T typedValue)
            return typedValue;

        var json = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<T>(json);
    }

    protected string GetClientId() => _configuration["LINKEDIN_CLIENT_ID"] ?? throw new InvalidOperationException("LINKEDIN_CLIENT_ID not configured");
    protected string GetClientSecret() => _configuration["LINKEDIN_CLIENT_SECRET"] ?? throw new InvalidOperationException("LINKEDIN_CLIENT_SECRET not configured");
    protected string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    protected class LinkedInTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;
    }

    protected class LinkedInUserInfo
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }

    protected class LinkedInMeResponse
    {
        [JsonPropertyName("vanityName")]
        public string? VanityName { get; set; }
    }

    protected class LinkedInUploadResponse
    {
        [JsonPropertyName("value")]
        public LinkedInUploadValue Value { get; set; } = new();
    }

    protected class LinkedInUploadValue
    {
        [JsonPropertyName("uploadUrl")]
        public string UploadUrl { get; set; } = string.Empty;

        [JsonPropertyName("image")]
        public string Image { get; set; } = string.Empty;
    }

    protected class LinkedInCommentResponse
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;
    }

    #endregion
}
