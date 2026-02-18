using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class FacebookProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "facebook";
    public override string Name => "Facebook Page";
    public override bool IsBetweenSteps => true;
    public override string[] Scopes => new[]
    {
        "pages_show_list",
        "business_management",
        "pages_manage_posts",
        "pages_manage_engagement",
        "pages_read_engagement",
        "read_insights"
    };
    public override int MaxConcurrentJobs => 100;

    public FacebookProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<FacebookProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 63206;

    protected override ErrorHandlingResult? HandleErrors(string responseBody)
    {
        if (responseBody.Contains("Error validating access token"))
        {
            return new ErrorHandlingResult(ErrorHandlingType.RefreshToken, "Please re-authenticate your Facebook account");
        }

        if (responseBody.Contains("490") || responseBody.Contains("REVOKED_ACCESS_TOKEN"))
        {
            return new ErrorHandlingResult(ErrorHandlingType.RefreshToken, "Access token expired, please re-authenticate");
        }

        if (responseBody.Contains("1366046"))
        {
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "Photos should be smaller than 4 MB and saved as JPG, PNG");
        }

        if (responseBody.Contains("1390008"))
        {
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "You are posting too fast, please slow down");
        }

        if (responseBody.Contains("1346003"))
        {
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "Content flagged as abusive by Facebook");
        }

        if (responseBody.Contains("1404078"))
        {
            return new ErrorHandlingResult(ErrorHandlingType.RefreshToken, "Page publishing authorization required, please re-authenticate");
        }

        return null;
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

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(6);
        var appId = GetAppId();
        var frontendUrl = GetFrontendUrl();

        var url = $"https://www.facebook.com/v20.0/dialog/oauth" +
                  $"?client_id={appId}" +
                  $"&redirect_uri={Uri.EscapeDataString($"{frontendUrl}/integrations/social/facebook")}" +
                  $"&state={state}" +
                  $"&scope={string.Join(",", Scopes)}";

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
        var redirectUri = $"{frontendUrl}/integrations/social/facebook";

        var shortLivedToken = await ExchangeCodeForTokenAsync(appId, appSecret, redirectUri, parameters.Code, cancellationToken);
        var longLivedToken = await ExchangeForLongLivedTokenAsync(appId, appSecret, shortLivedToken, cancellationToken);

        var permissions = await GetPermissionsAsync(longLivedToken, cancellationToken);
        CheckScopes(Scopes, permissions);

        var userInfo = await GetUserInfoAsync(longLivedToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.Name,
            AccessToken = longLivedToken,
            RefreshToken = longLivedToken,
            ExpiresIn = (int)TimeSpan.FromDays(59).TotalSeconds,
            Picture = userInfo.Picture?.Data?.Url ?? string.Empty,
            Username = string.Empty
        };
    }

    public override async Task<AuthTokenDetails?> ReConnectAsync(
        string id,
        string requiredId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var page = await FetchPageInformationAsync(accessToken, new { page = requiredId }, cancellationToken);

        return new AuthTokenDetails
        {
            Id = page.Id,
            Name = page.Name,
            AccessToken = page.AccessToken,
            Picture = page.Picture,
            Username = page.Username
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
        var linkUrl = GetSettingValue<string>(settings, "url");

        if (firstPost.Media != null && firstPost.Media.Count > 0)
        {
            var media = firstPost.Media[0];
            if (media.Path.Contains(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                return await PostVideoAsync(id, accessToken, firstPost, cancellationToken);
            }
        }

        return await PostFeedAsync(id, accessToken, firstPost, linkUrl, cancellationToken);
    }

    private async Task<PostResponse[]> PostFeedAsync(
        string pageId,
        string accessToken,
        PostDetails post,
        string? linkUrl,
        CancellationToken cancellationToken)
    {
        var uploadedPhotoIds = new List<string>();

        if (post.Media != null && post.Media.Count > 0)
        {
            foreach (var media in post.Media)
            {
                var photoId = await UploadPhotoAsync(pageId, accessToken, media.Path, false, cancellationToken);
                uploadedPhotoIds.Add(photoId);
            }
        }

        var payload = new Dictionary<string, object?>
        {
            ["message"] = post.Message,
            ["published"] = true
        };

        if (uploadedPhotoIds.Count > 0)
        {
            payload["attached_media"] = uploadedPhotoIds.Select(id => new { media_fbid = id }).ToArray();
        }

        if (!string.IsNullOrEmpty(linkUrl))
        {
            payload["link"] = linkUrl;
        }

        var url = $"https://graph.facebook.com/v20.0/{pageId}/feed?access_token={accessToken}&fields=id,permalink_url";

        var request = CreateRequest(url, HttpMethod.Post, payload);
        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var postResponse = await DeserializeAsync<FacebookPostResponse>(response);

        return new[]
        {
            new PostResponse
            {
                Id = post.Id,
                PostId = postResponse.Id,
                ReleaseUrl = postResponse.PermalinkUrl ?? string.Empty,
                Status = "success"
            }
        };
    }

    private async Task<PostResponse[]> PostVideoAsync(
        string pageId,
        string accessToken,
        PostDetails post,
        CancellationToken cancellationToken)
    {
        var media = post.Media![0];

        var payload = new Dictionary<string, object?>
        {
            ["file_url"] = media.Path,
            ["description"] = post.Message,
            ["published"] = true
        };

        var url = $"https://graph.facebook.com/v20.0/{pageId}/videos?access_token={accessToken}&fields=id,permalink_url";

        var request = CreateRequest(url, HttpMethod.Post, payload);
        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var videoResponse = await DeserializeAsync<FacebookVideoResponse>(response);

        return new[]
        {
            new PostResponse
            {
                Id = post.Id,
                PostId = videoResponse.Id,
                ReleaseUrl = $"https://www.facebook.com/reel/{videoResponse.Id}",
                Status = "success"
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
        var replyToId = lastCommentId ?? postId;

        var payload = new Dictionary<string, object?>
        {
            ["message"] = commentPost.Message
        };

        if (commentPost.Media != null && commentPost.Media.Count > 0)
        {
            payload["attachment_url"] = commentPost.Media[0].Path;
        }

        var url = $"https://graph.facebook.com/v20.0/{replyToId}/comments?access_token={accessToken}&fields=id,permalink_url";

        var request = CreateRequest(url, HttpMethod.Post, payload);
        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var commentResponse = await DeserializeAsync<FacebookPostResponse>(response);

        return new[]
        {
            new PostResponse
            {
                Id = commentPost.Id,
                PostId = commentResponse.Id,
                ReleaseUrl = commentResponse.PermalinkUrl ?? string.Empty,
                Status = "success"
            }
        };
    }

    public async Task<List<FacebookPage>> GetPagesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var url = $"https://graph.facebook.com/v20.0/me/accounts?fields=id,username,name,picture.type(large)&access_token={accessToken}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var pagesResponse = await DeserializeAsync<FacebookPagesResponse>(response);
        return pagesResponse.Data ?? new List<FacebookPage>();
    }

    public override async Task<FetchPageInformationResult?> FetchPageInformationAsync(
        string accessToken,
        object data,
        CancellationToken cancellationToken = default)
    {
        var pageId = data.GetType().GetProperty("page")?.GetValue(data)?.ToString() ?? string.Empty;

        var url = $"https://graph.facebook.com/v20.0/{pageId}?fields=username,access_token,name,picture.type(large)&access_token={accessToken}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var page = await DeserializeAsync<FacebookPageDetail>(response);

        return new FetchPageInformationResult
        {
            Id = page.Id,
            Name = page.Name,
            AccessToken = page.AccessToken,
            Picture = page.Picture?.Data?.Url ?? string.Empty,
            Username = page.Username ?? string.Empty
        };
    }

    private async Task<string> UploadPhotoAsync(string pageId, string accessToken, string photoUrl, bool published, CancellationToken cancellationToken)
    {
        var url = $"https://graph.facebook.com/v20.0/{pageId}/photos?access_token={accessToken}";

        var payload = new
        {
            url = photoUrl,
            published
        };

        var request = CreateRequest(url, HttpMethod.Post, payload);
        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var photoResponse = await DeserializeAsync<FacebookPhotoResponse>(response);
        return photoResponse.Id;
    }

    private async Task<string> ExchangeCodeForTokenAsync(string appId, string appSecret, string redirectUri, string code, CancellationToken cancellationToken)
    {
        var url = $"https://graph.facebook.com/v20.0/oauth/access_token" +
                  $"?client_id={appId}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&client_secret={appSecret}" +
                  $"&code={code}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await DeserializeAsync<FacebookAccessTokenResponse>(response);
        return tokenResponse.AccessToken;
    }

    private async Task<string> ExchangeForLongLivedTokenAsync(string appId, string appSecret, string shortLivedToken, CancellationToken cancellationToken)
    {
        var url = $"https://graph.facebook.com/v20.0/oauth/access_token" +
                  $"?grant_type=fb_exchange_token" +
                  $"&client_id={appId}" +
                  $"&client_secret={appSecret}" +
                  $"&fb_exchange_token={shortLivedToken}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await DeserializeAsync<FacebookAccessTokenResponse>(response);
        return tokenResponse.AccessToken;
    }

    private async Task<string[]> GetPermissionsAsync(string accessToken, CancellationToken cancellationToken)
    {
        var url = $"https://graph.facebook.com/v20.0/me/permissions?access_token={accessToken}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var permissionsResponse = await DeserializeAsync<FacebookPermissionsResponse>(response);
        return permissionsResponse.Data
            .Where(p => p.Status == "granted")
            .Select(p => p.Permission)
            .ToArray();
    }

    private async Task<FacebookUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var url = $"https://graph.facebook.com/v20.0/me?fields=id,name,picture&access_token={accessToken}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<FacebookUserInfo>(response);
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

    private string GetAppId() => _configuration["FACEBOOK_APP_ID"] ?? throw new InvalidOperationException("FACEBOOK_APP_ID not configured");
    private string GetAppSecret() => _configuration["FACEBOOK_APP_SECRET"] ?? throw new InvalidOperationException("FACEBOOK_APP_SECRET not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    private class FacebookAccessTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }

    private class FacebookPermissionsResponse
    {
        [JsonPropertyName("data")]
        public List<FacebookPermission> Data { get; set; } = new();
    }

    private class FacebookPermission
    {
        [JsonPropertyName("permission")]
        public string Permission { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    private class FacebookUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("picture")]
        public FacebookPicture? Picture { get; set; }
    }

    private class FacebookPicture
    {
        [JsonPropertyName("data")]
        public FacebookPictureData? Data { get; set; }
    }

    private class FacebookPictureData
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    private class FacebookPostResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("permalink_url")]
        public string? PermalinkUrl { get; set; }
    }

    private class FacebookVideoResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class FacebookPhotoResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class FacebookPagesResponse
    {
        [JsonPropertyName("data")]
        public List<FacebookPage>? Data { get; set; }
    }

    public class FacebookPage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }

    private class FacebookPageDetail : FacebookPage
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("picture")]
        public FacebookPicture? Picture { get; set; }
    }

    #endregion
}
