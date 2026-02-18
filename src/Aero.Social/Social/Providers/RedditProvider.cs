using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class RedditProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "reddit";
    public override string Name => "Reddit";
    public override string[] Scopes => new[] { "read", "identity", "submit", "flair" };
    public override int MaxConcurrentJobs => 1;

    public RedditProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<RedditProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 10000;

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(6);
        var codeVerifier = MakeId(30);
        var clientId = GetClientId();
        var frontendUrl = GetFrontendUrl();

        var url = $"https://www.reddit.com/api/v1/authorize" +
                  $"?client_id={clientId}" +
                  $"&response_type=code" +
                  $"&state={state}" +
                  $"&redirect_uri={Uri.EscapeDataString($"{frontendUrl}/integrations/social/reddit")}" +
                  $"&duration=permanent" +
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
        var redirectUri = $"{frontendUrl}/integrations/social/reddit";

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = parameters.Code,
            ["redirect_uri"] = redirectUri
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://www.reddit.com/api/v1/access_token")
        {
            Content = content
        };
        request.Headers.Add("Authorization", $"Basic {credentials}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await DeserializeAsync<RedditTokenResponse>(response);
        CheckScopes(Scopes, tokenResponse.Scope);

        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.Name,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresIn = tokenResponse.ExpiresIn,
            Picture = GetCleanIconUrl(userInfo.IconImg),
            Username = userInfo.Name
        };
    }

    public override async Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetClientId();
        var clientSecret = GetClientSecret();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://www.reddit.com/api/v1/access_token")
        {
            Content = content
        };
        request.Headers.Add("Authorization", $"Basic {credentials}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await DeserializeAsync<RedditTokenResponse>(response);
        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.Name,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresIn = tokenResponse.ExpiresIn,
            Picture = GetCleanIconUrl(userInfo.IconImg),
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
        var settings = firstPost.Settings ?? new Dictionary<string, object>();

        var subreddits = GetSettingValue<List<Dictionary<string, object>>>(settings, "subreddit") ?? new List<Dictionary<string, object>>();

        var results = new List<PostResponse>();

        foreach (var subredditConfig in subreddits)
        {
            var subredditSettings = GetSettingValue<Dictionary<string, object>>(subredditConfig, "value") ?? new Dictionary<string, object>();
            var subreddit = GetSettingValue<string>(subredditSettings, "subreddit") ?? string.Empty;
            var title = GetSettingValue<string>(subredditSettings, "title") ?? string.Empty;
            var postType = GetSettingValue<string>(subredditSettings, "type") ?? "self";
            var flairId = GetSettingValue<string>(subredditSettings, "flair_id");
            var url = GetSettingValue<string>(subredditSettings, "url");

            var postData = new Dictionary<string, string>
            {
                ["api_type"] = "json",
                ["title"] = title,
                ["sr"] = subreddit,
                ["text"] = firstPost.Message
            };

            if (!string.IsNullOrEmpty(flairId))
            {
                postData["flair_id"] = flairId;
            }

            if (postType == "link" && !string.IsNullOrEmpty(url))
            {
                postData["kind"] = "link";
                postData["url"] = url;
            }
            else if (postType == "media" && firstPost.Media != null && firstPost.Media.Count > 0)
            {
                var media = firstPost.Media[0];
                var isVideo = media.Path.Contains(".mp4", StringComparison.OrdinalIgnoreCase);

                var uploadedUrl = await UploadFileToRedditAsync(accessToken, media.Path, cancellationToken);

                postData["kind"] = isVideo ? "video" : "image";
                postData["url"] = uploadedUrl;

                if (isVideo && !string.IsNullOrEmpty(media.Thumbnail))
                {
                    var thumbnailUrl = await UploadFileToRedditAsync(accessToken, media.Thumbnail, cancellationToken);
                    postData["video_poster_url"] = thumbnailUrl;
                }
            }
            else
            {
                postData["kind"] = "self";
            }

            var content = new FormUrlEncodedContent(postData);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.reddit.com/api/submit")
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var submitResponse = await DeserializeAsync<RedditSubmitResponse>(response);

            string postId;
            string postUrl;

            if (submitResponse.Json?.Data?.Id != null)
            {
                postId = submitResponse.Json.Data.Id;
                postUrl = submitResponse.Json.Data.Url;
            }
            else if (!string.IsNullOrEmpty(submitResponse.Json?.Data?.WebsocketUrl))
            {
                (postId, postUrl) = await WaitForWebSocketResponseAsync(submitResponse.Json.Data.WebsocketUrl, cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("Failed to submit Reddit post");
            }

            results.Add(new PostResponse
            {
                Id = firstPost.Id,
                PostId = postId,
                ReleaseUrl = postUrl,
                Status = "published"
            });

            if (subreddits.Count > 1)
            {
                await Task.Delay(5000, cancellationToken);
            }
        }

        return results.GroupBy(r => r.Id)
            .Select(g => new PostResponse
            {
                Id = g.Key,
                PostId = string.Join(",", g.Select(r => r.PostId)),
                ReleaseUrl = string.Join(",", g.Select(r => r.ReleaseUrl)),
                Status = "published"
            })
            .ToArray();
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
        var thingId = postId.StartsWith("t3_") ? postId : $"t3_{postId}";

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["text"] = commentPost.Message,
            ["thing_id"] = thingId,
            ["api_type"] = "json"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.reddit.com/api/comment")
        {
            Content = content
        };
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var commentResponse = await DeserializeAsync<RedditCommentResponse>(response);

        var commentId = commentResponse.Json?.Data?.Things?.FirstOrDefault()?.Data?.Id ?? string.Empty;
        var permalink = commentResponse.Json?.Data?.Things?.FirstOrDefault()?.Data?.Permalink ?? string.Empty;

        return new[]
        {
            new PostResponse
            {
                Id = commentPost.Id,
                PostId = commentId,
                ReleaseUrl = $"https://www.reddit.com{permalink}",
                Status = "published"
            }
        };
    }

    public async Task<List<RedditSubreddit>> SearchSubredditsAsync(string accessToken, string query, CancellationToken cancellationToken = default)
    {
        var url = $"https://oauth.reddit.com/subreddits/search?show=public&q={Uri.EscapeDataString(query)}&sort=activity&show_users=false&limit=10";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var searchResponse = await DeserializeAsync<RedditSubredditSearchResponse>(response);

        return searchResponse.Data?.Children?
            .Where(c => c.Data?.SubredditType == "public" && c.Data?.SubmissionType != "image")
            .Select(c => new RedditSubreddit
            {
                Id = c.Data?.Id ?? string.Empty,
                Title = c.Data?.Title ?? string.Empty,
                Name = c.Data?.Url ?? string.Empty
            })
            .ToList() ?? new List<RedditSubreddit>();
    }

    private async Task<string> UploadFileToRedditAsync(string accessToken, string filePath, CancellationToken cancellationToken)
    {
        var fileName = filePath.Split('/').Last();
        var mimeType = GetMimeType(fileName);

        var formData = new MultipartFormDataContent
        {
            { new StringContent(fileName), "filepath" },
            { new StringContent(mimeType), "mimetype" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.reddit.com/api/media/asset")
        {
            Content = formData
        };
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var assetResponse = await DeserializeAsync<RedditAssetResponse>(response);

        var mediaBytes = await ReadOrFetchAsync(filePath, cancellationToken);

        var uploadForm = new MultipartFormDataContent();
        foreach (var field in assetResponse.Args.Fields)
        {
            uploadForm.Add(new StringContent(field.Value), field.Name);
        }
        uploadForm.Add(new ByteArrayContent(mediaBytes), "file", fileName);

        var uploadResponse = await HttpClient.PostAsync("https:" + assetResponse.Args.Action, uploadForm, cancellationToken);
        var uploadResult = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);

        var locationMatch = System.Text.RegularExpressions.Regex.Match(uploadResult, @"<Location>(.*?)</Location>");
        return locationMatch.Success ? locationMatch.Groups[1].Value : string.Empty;
    }

    private async Task<(string id, string url)> WaitForWebSocketResponseAsync(string websocketUrl, CancellationToken cancellationToken)
    {
        await Task.Delay(2000, cancellationToken);
        return (string.Empty, string.Empty);
    }

    private async Task<RedditUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://oauth.reddit.com/api/v1/me");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<RedditUserInfo>(response);
    }

    private static string GetCleanIconUrl(string? iconUrl)
    {
        if (string.IsNullOrEmpty(iconUrl))
            return string.Empty;

        var questionIndex = iconUrl.IndexOf('?');
        return questionIndex > 0 ? iconUrl.Substring(0, questionIndex) : iconUrl;
    }

    private static string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
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

    private string GetClientId() => _configuration["REDDIT_CLIENT_ID"] ?? throw new InvalidOperationException("REDDIT_CLIENT_ID not configured");
    private string GetClientSecret() => _configuration["REDDIT_CLIENT_SECRET"] ?? throw new InvalidOperationException("REDDIT_CLIENT_SECRET not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    private class RedditTokenResponse
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

    private class RedditUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("icon_img")]
        public string? IconImg { get; set; }
    }

    private class RedditSubmitResponse
    {
        [JsonPropertyName("json")]
        public RedditSubmitJson? Json { get; set; }
    }

    private class RedditSubmitJson
    {
        [JsonPropertyName("data")]
        public RedditSubmitData? Data { get; set; }
    }

    private class RedditSubmitData
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("websocket_url")]
        public string? WebsocketUrl { get; set; }
    }

    private class RedditCommentResponse
    {
        [JsonPropertyName("json")]
        public RedditCommentJson? Json { get; set; }
    }

    private class RedditCommentJson
    {
        [JsonPropertyName("data")]
        public RedditCommentData? Data { get; set; }
    }

    private class RedditCommentData
    {
        [JsonPropertyName("things")]
        public List<RedditCommentThing>? Things { get; set; }
    }

    private class RedditCommentThing
    {
        [JsonPropertyName("data")]
        public RedditCommentThingData? Data { get; set; }
    }

    private class RedditCommentThingData
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("permalink")]
        public string? Permalink { get; set; }
    }

    private class RedditAssetResponse
    {
        [JsonPropertyName("args")]
        public RedditAssetArgs Args { get; set; } = new();
    }

    private class RedditAssetArgs
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("fields")]
        public List<RedditAssetField> Fields { get; set; } = new();
    }

    private class RedditAssetField
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }

    private class RedditSubredditSearchResponse
    {
        [JsonPropertyName("data")]
        public RedditSubredditSearchData? Data { get; set; }
    }

    private class RedditSubredditSearchData
    {
        [JsonPropertyName("children")]
        public List<RedditSubredditChild>? Children { get; set; }
    }

    private class RedditSubredditChild
    {
        [JsonPropertyName("data")]
        public RedditSubredditChildData? Data { get; set; }
    }

    private class RedditSubredditChildData
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("subreddit_type")]
        public string? SubredditType { get; set; }

        [JsonPropertyName("submission_type")]
        public string? SubmissionType { get; set; }
    }

    public class RedditSubreddit
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
