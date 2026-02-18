using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class ThreadsProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "threads";
    public override string Name => "Threads";
    public override string[] Scopes => new[]
    {
        "threads_basic",
        "threads_content_publish",
        "threads_manage_replies",
        "threads_manage_insights"
    };
    public override int MaxConcurrentJobs => 2;

    public ThreadsProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ThreadsProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 500;

    protected override ErrorHandlingResult? HandleErrors(string responseBody)
    {
        if (responseBody.Contains("Error validating access token"))
        {
            return new ErrorHandlingResult(ErrorHandlingType.RefreshToken, "Threads access token expired");
        }

        return null;
    }

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(6);
        var appId = GetAppId();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = $"{frontendUrl}/integrations/social/threads";

        var url = "https://www.threads.net/oauth/authorize" +
                  $"?client_id={appId}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&state={state}" +
                  $"&scope={Uri.EscapeDataString(string.Join(",", Scopes))}";

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
        var redirectUri = $"{frontendUrl}/integrations/social/threads";

        var shortLivedToken = await ExchangeCodeForTokenAsync(appId, appSecret, redirectUri, parameters.Code, cancellationToken);
        var longLivedToken = await ExchangeForLongLivedTokenAsync(appId, appSecret, shortLivedToken, cancellationToken);
        var userInfo = await FetchUserInfoAsync(longLivedToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.Name,
            AccessToken = longLivedToken,
            RefreshToken = longLivedToken,
            ExpiresIn = (int)TimeSpan.FromDays(58).TotalSeconds,
            Picture = userInfo.Picture ?? string.Empty,
            Username = userInfo.Username
        };
    }

    public override async Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://graph.threads.net/refresh_access_token?grant_type=th_refresh_token&access_token={refreshToken}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await DeserializeAsync<ThreadsTokenResponse>(response);
        var userInfo = await FetchUserInfoAsync(tokenResponse.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.Name,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.AccessToken,
            ExpiresIn = (int)TimeSpan.FromDays(58).TotalSeconds,
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

        var creationId = await CreateThreadContentAsync(id, accessToken, firstPost, null, cancellationToken);
        var (threadId, permalink) = await PublishThreadAsync(id, accessToken, creationId, cancellationToken);

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = threadId,
                Status = "success",
                ReleaseUrl = permalink
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
        var replyToId = lastCommentId ?? postId;

        var creationId = await CreateThreadContentAsync(id, accessToken, commentPost, replyToId, cancellationToken);
        var (threadId, permalink) = await PublishThreadAsync(id, accessToken, creationId, cancellationToken);

        return new[]
        {
            new PostResponse
            {
                Id = commentPost.Id,
                PostId = threadId,
                Status = "success",
                ReleaseUrl = permalink
            }
        };
    }

    public override async Task<AnalyticsData[]?> AnalyticsAsync(
        string id,
        string accessToken,
        int days,
        CancellationToken cancellationToken = default)
    {
        var until = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var since = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeSeconds();

        var url = $"https://graph.threads.net/v1.0/{id}/threads_insights" +
                  $"?metric=views,likes,replies,reposts,quotes" +
                  $"&access_token={accessToken}" +
                  $"&period=day&since={since}&until={until}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var insightsResponse = await DeserializeAsync<ThreadsInsightsResponse>(response);

        return insightsResponse.Data?.Select(d => new AnalyticsData
        {
            Label = Capitalize(d.Name ?? ""),
            PercentageChange = 5,
            Data = d.TotalValue != null
                ? new List<AnalyticsDataPoint> { new() { Total = d.TotalValue.Value.ToString(), Date = DateTime.UtcNow.ToString("yyyy-MM-dd") } }
                : d.Values?.Select(v => new AnalyticsDataPoint { Total = v.Value?.ToString() ?? "0", Date = v.EndTime?.ToString("yyyy-MM-dd") ?? "" }).ToList() ?? new List<AnalyticsDataPoint>()
        }).ToArray() ?? Array.Empty<AnalyticsData>();
    }

    public override async Task<AnalyticsData[]?> PostAnalyticsAsync(
        string integrationId,
        string accessToken,
        string postId,
        int days,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var url = $"https://graph.threads.net/v1.0/{postId}/insights" +
                  $"?metric=views,likes,replies,reposts,quotes" +
                  $"&access_token={accessToken}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var insightsResponse = await DeserializeAsync<ThreadsInsightsResponse>(response);

        if (insightsResponse.Data == null || insightsResponse.Data.Count == 0)
            return Array.Empty<AnalyticsData>();

        var result = new List<AnalyticsData>();

        foreach (var metric in insightsResponse.Data)
        {
            var value = metric.Values?.FirstOrDefault()?.Value ?? metric.TotalValue?.Value;
            if (value == null) continue;

            var label = metric.Name switch
            {
                "views" => "Views",
                "likes" => "Likes",
                "replies" => "Replies",
                "reposts" => "Reposts",
                "quotes" => "Quotes",
                _ => ""
            };

            if (!string.IsNullOrEmpty(label))
            {
                result.Add(new AnalyticsData
                {
                    Label = label,
                    PercentageChange = 0,
                    Data = new List<AnalyticsDataPoint> { new() { Total = value.ToString() ?? "0", Date = today } }
                });
            }
        }

        return result.ToArray();
    }

    private async Task<string> CreateThreadContentAsync(
        string userId,
        string accessToken,
        PostDetails post,
        string? replyToId,
        CancellationToken cancellationToken)
    {
        if (post.Media == null || post.Media.Count == 0)
        {
            return await CreateTextContentAsync(userId, accessToken, post.Message, replyToId, null, cancellationToken);
        }

        if (post.Media.Count == 1)
        {
            return await CreateSingleMediaContentAsync(userId, accessToken, post.Media[0], post.Message, false, replyToId, cancellationToken);
        }

        return await CreateCarouselContentAsync(userId, accessToken, post.Media, post.Message, replyToId, cancellationToken);
    }

    private async Task<string> CreateTextContentAsync(
        string userId,
        string accessToken,
        string message,
        string? replyToId,
        string? quoteId,
        CancellationToken cancellationToken)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent("TEXT"), "media_type");
        form.Add(new StringContent(message), "text");
        form.Add(new StringContent(accessToken), "access_token");

        if (!string.IsNullOrEmpty(replyToId))
        {
            form.Add(new StringContent(replyToId), "reply_to_id");
        }

        if (!string.IsNullOrEmpty(quoteId))
        {
            form.Add(new StringContent(quoteId), "quote_post_id");
        }

        var url = $"https://graph.threads.net/v1.0/{userId}/threads";
        var response = await HttpClient.PostAsync(url, form, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentResponse = await DeserializeAsync<ThreadsContentResponse>(response);
        return contentResponse.Id;
    }

    private async Task<string> CreateSingleMediaContentAsync(
        string userId,
        string accessToken,
        MediaContent media,
        string message,
        bool isCarouselItem,
        string? replyToId,
        CancellationToken cancellationToken)
    {
        var isVideo = media.Path.Contains(".mp4", StringComparison.OrdinalIgnoreCase);
        var mediaParams = new Dictionary<string, string>
        {
            ["media_type"] = isVideo ? "VIDEO" : "IMAGE",
            [isVideo ? "video_url" : "image_url"] = media.Path,
            ["text"] = message,
            ["access_token"] = accessToken
        };

        if (isCarouselItem)
        {
            mediaParams["is_carousel_item"] = "true";
        }

        if (!string.IsNullOrEmpty(replyToId))
        {
            mediaParams["reply_to_id"] = replyToId;
        }

        var queryString = string.Join("&", mediaParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var url = $"https://graph.threads.net/v1.0/{userId}/threads?{queryString}";

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentResponse = await DeserializeAsync<ThreadsContentResponse>(response);
        return contentResponse.Id;
    }

    private async Task<string> CreateCarouselContentAsync(
        string userId,
        string accessToken,
        List<MediaContent> media,
        string message,
        string? replyToId,
        CancellationToken cancellationToken)
    {
        var mediaIds = new List<string>();

        foreach (var mediaItem in media)
        {
            var mediaId = await CreateSingleMediaContentAsync(userId, accessToken, mediaItem, message, true, null, cancellationToken);
            mediaIds.Add(mediaId);
        }

        foreach (var mediaId in mediaIds)
        {
            await CheckMediaLoadedAsync(mediaId, accessToken, cancellationToken);
        }

        var carouselParams = new Dictionary<string, string>
        {
            ["text"] = message,
            ["media_type"] = "CAROUSEL",
            ["children"] = string.Join(",", mediaIds),
            ["access_token"] = accessToken
        };

        if (!string.IsNullOrEmpty(replyToId))
        {
            carouselParams["reply_to_id"] = replyToId;
        }

        var queryString = string.Join("&", carouselParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var url = $"https://graph.threads.net/v1.0/{userId}/threads?{queryString}";

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentResponse = await DeserializeAsync<ThreadsContentResponse>(response);
        return contentResponse.Id;
    }

    private async Task CheckMediaLoadedAsync(string mediaContainerId, string accessToken, CancellationToken cancellationToken)
    {
        var url = $"https://graph.threads.net/v1.0/{mediaContainerId}?fields=status,error_message&access_token={accessToken}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var statusResponse = await DeserializeAsync<ThreadsMediaStatusResponse>(response);

        if (statusResponse.Status == "ERROR")
        {
            throw new BadBodyException(Identifier, statusResponse.ErrorMessage ?? "Unknown error");
        }

        if (statusResponse.Status == "FINISHED")
        {
            await Task.Delay(2000, cancellationToken);
            return;
        }

        await Task.Delay(2200, cancellationToken);
        await CheckMediaLoadedAsync(mediaContainerId, accessToken, cancellationToken);
    }

    private async Task<(string ThreadId, string Permalink)> PublishThreadAsync(
        string userId,
        string accessToken,
        string creationId,
        CancellationToken cancellationToken)
    {
        await CheckMediaLoadedAsync(creationId, accessToken, cancellationToken);

        var publishUrl = $"https://graph.threads.net/v1.0/{userId}/threads_publish?creation_id={creationId}&access_token={accessToken}";

        var publishRequest = new HttpRequestMessage(HttpMethod.Post, publishUrl);
        var publishResponse = await HttpClient.SendAsync(publishRequest, cancellationToken);
        publishResponse.EnsureSuccessStatusCode();

        var publishResult = await DeserializeAsync<ThreadsContentResponse>(publishResponse);

        var permalinkUrl = $"https://graph.threads.net/v1.0/{publishResult.Id}?fields=id,permalink&access_token={accessToken}";
        var permalinkResponse = await HttpClient.GetAsync(permalinkUrl, cancellationToken);
        permalinkResponse.EnsureSuccessStatusCode();

        var permalinkResult = await DeserializeAsync<ThreadsPermalinkResponse>(permalinkResponse);

        return (publishResult.Id, permalinkResult.Permalink ?? "");
    }

    private async Task<string> ExchangeCodeForTokenAsync(string appId, string appSecret, string redirectUri, string code, CancellationToken cancellationToken)
    {
        var url = "https://graph.threads.net/oauth/access_token" +
                  $"?client_id={appId}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&grant_type=authorization_code" +
                  $"&client_secret={appSecret}" +
                  $"&code={code}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await DeserializeAsync<ThreadsTokenResponse>(response);
        return tokenResponse.AccessToken;
    }

    private async Task<string> ExchangeForLongLivedTokenAsync(string appId, string appSecret, string shortLivedToken, CancellationToken cancellationToken)
    {
        var url = "https://graph.threads.net/access_token" +
                  "?grant_type=th_exchange_token" +
                  $"&client_secret={appSecret}" +
                  $"&access_token={shortLivedToken}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await DeserializeAsync<ThreadsTokenResponse>(response);
        return tokenResponse.AccessToken;
    }

    private async Task<ThreadsUserInfo> FetchUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var url = $"https://graph.threads.net/v1.0/me?fields=id,username,threads_profile_picture_url&access_token={accessToken}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<ThreadsUserInfo>(response);
    }

    private static string Capitalize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpper(input[0]) + input.Substring(1);
    }

    private string GetAppId() => _configuration["THREADS_APP_ID"] ?? throw new InvalidOperationException("THREADS_APP_ID not configured");
    private string GetAppSecret() => _configuration["THREADS_APP_SECRET"] ?? throw new InvalidOperationException("THREADS_APP_SECRET not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    private class ThreadsTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }

    private class ThreadsUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("threads_profile_picture_url")]
        public string? Picture { get; set; }

        [JsonIgnore]
        public string Name => Username;
    }

    private class ThreadsContentResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class ThreadsMediaStatusResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }

    private class ThreadsPermalinkResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("permalink")]
        public string? Permalink { get; set; }
    }

    private class ThreadsInsightsResponse
    {
        [JsonPropertyName("data")]
        public List<ThreadsInsightMetric>? Data { get; set; }
    }

    private class ThreadsInsightMetric
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("total_value")]
        public ThreadsTotalValue? TotalValue { get; set; }

        [JsonPropertyName("values")]
        public List<ThreadsMetricValue>? Values { get; set; }
    }

    private class ThreadsTotalValue
    {
        [JsonPropertyName("value")]
        public long? Value { get; set; }
    }

    private class ThreadsMetricValue
    {
        [JsonPropertyName("value")]
        public long? Value { get; set; }

        [JsonPropertyName("end_time")]
        public DateTime? EndTime { get; set; }
    }

    #endregion
}
