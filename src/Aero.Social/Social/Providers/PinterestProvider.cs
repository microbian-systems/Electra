using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class PinterestProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "pinterest";
    public override string Name => "Pinterest";
    public override string[] Scopes => new[]
    {
        "boards:read",
        "boards:write",
        "pins:read",
        "pins:write",
        "user_accounts:read"
    };
    public override int MaxConcurrentJobs => 3;

    public PinterestProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<PinterestProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 500;

    protected override ErrorHandlingResult? HandleErrors(string responseBody)
    {
        if (responseBody.Contains("cover_image_url or cover_image_content_type"))
        {
            return new ErrorHandlingResult(
                ErrorHandlingType.BadBody,
                "When uploading a video, you must add also an image to be used as a cover image.");
        }

        return null;
    }

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(6);
        var clientId = GetClientId();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = $"{frontendUrl}/integrations/social/pinterest";

        var url = "https://www.pinterest.com/oauth/" +
                  $"?client_id={clientId}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&response_type=code" +
                  $"&scope={Uri.EscapeDataString(string.Join(",", Scopes))}" +
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
        var redirectUri = $"{frontendUrl}/integrations/social/pinterest";

        var tokenUrl = "https://api.pinterest.com/v5/oauth/token";
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = parameters.Code,
            ["redirect_uri"] = redirectUri
        };

        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Basic {credentials}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenInfo = await DeserializeAsync<PinterestTokenResponse>(response);
        CheckScopes(Scopes, tokenInfo.Scope ?? "");

        var userInfo = await FetchUserInfoAsync(tokenInfo.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.Username,
            AccessToken = tokenInfo.AccessToken,
            RefreshToken = tokenInfo.RefreshToken ?? string.Empty,
            ExpiresIn = tokenInfo.ExpiresIn ?? 0,
            Picture = userInfo.ProfileImage ?? string.Empty,
            Username = userInfo.Username
        };
    }

    public override async Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetClientId();
        var clientSecret = GetClientSecret();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = $"{frontendUrl}/integrations/social/pinterest";

        var tokenUrl = "https://api.pinterest.com/v5/oauth/token";
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["scope"] = string.Join(",", Scopes),
            ["redirect_uri"] = redirectUri
        };

        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Basic {credentials}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenInfo = await DeserializeAsync<PinterestTokenResponse>(response);
        var userInfo = await FetchUserInfoAsync(tokenInfo.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.Username,
            AccessToken = tokenInfo.AccessToken,
            RefreshToken = refreshToken,
            ExpiresIn = tokenInfo.ExpiresIn ?? 0,
            Picture = userInfo.ProfileImage ?? string.Empty,
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
        var settings = firstPost.Settings ?? new Dictionary<string, object>();

        var videoMedia = firstPost.Media?.FirstOrDefault(m => m.Path.Contains(".mp4", StringComparison.OrdinalIgnoreCase));
        var pictureMedia = firstPost.Media?.FirstOrDefault(m => !m.Path.Contains(".mp4", StringComparison.OrdinalIgnoreCase));

        string? mediaId = null;

        if (videoMedia != null)
        {
            mediaId = await UploadVideoAsync(accessToken, videoMedia.Path, cancellationToken);
        }

        var mapImages = firstPost.Media?
            .Where(m => !m.Path.Contains(".mp4", StringComparison.OrdinalIgnoreCase))
            .Select(m => new { path = m.Path })
            .ToList();

        var boardId = GetSettingValue<string>(settings, "board") ?? "";
        var link = GetSettingValue<string>(settings, "link");
        var title = GetSettingValue<string>(settings, "title");
        var dominantColor = GetSettingValue<string>(settings, "dominant_color");

        var pinPayload = new Dictionary<string, object?>();

        if (!string.IsNullOrEmpty(link))
            pinPayload["link"] = link;

        if (!string.IsNullOrEmpty(title))
            pinPayload["title"] = title;

        pinPayload["description"] = firstPost.Message;

        if (!string.IsNullOrEmpty(dominantColor))
            pinPayload["dominant_color"] = dominantColor;

        pinPayload["board_id"] = boardId;

        if (mediaId != null)
        {
            pinPayload["media_source"] = new
            {
                source_type = "video_id",
                media_id = mediaId,
                cover_image_url = pictureMedia?.Path
            };
        }
        else if (mapImages != null && mapImages.Count == 1)
        {
            pinPayload["media_source"] = new
            {
                source_type = "image_url",
                url = mapImages[0].path
            };
        }
        else if (mapImages != null && mapImages.Count > 1)
        {
            pinPayload["media_source"] = new
            {
                source_type = "multiple_image_urls",
                items = mapImages
            };
        }

        var pinUrl = "https://api.pinterest.com/v5/pins";
        var pinRequest = CreateJsonRequest(pinUrl, HttpMethod.Post, pinPayload, accessToken);
        var pinResponse = await HttpClient.SendAsync(pinRequest, cancellationToken);
        pinResponse.EnsureSuccessStatusCode();

        var pinResult = await DeserializeAsync<PinterestPinResponse>(pinResponse);

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = pinResult.Id,
                ReleaseUrl = $"https://www.pinterest.com/pin/{pinResult.Id}",
                Status = "success"
            }
        };
    }

    public override async Task<AnalyticsData[]?> AnalyticsAsync(
        string id,
        string accessToken,
        int days,
        CancellationToken cancellationToken = default)
    {
        var until = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var since = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");

        var url = $"https://api.pinterest.com/v5/user_account/analytics?start_date={since}&end_date={until}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        request.Headers.TryAddWithoutValidation("Content-Type", "application/json");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var analyticsResponse = await DeserializeAsync<PinterestAnalyticsResponse>(response);

        if (analyticsResponse.All?.DailyMetrics == null)
            return Array.Empty<AnalyticsData>();

        var result = new List<AnalyticsData>
        {
            new() { Label = "Pin click rate", Data = new List<AnalyticsDataPoint>() },
            new() { Label = "Impressions", Data = new List<AnalyticsDataPoint>() },
            new() { Label = "Pin Clicks", Data = new List<AnalyticsDataPoint>() },
            new() { Label = "Engagement", Data = new List<AnalyticsDataPoint>() },
            new() { Label = "Saves", Data = new List<AnalyticsDataPoint>() }
        };

        foreach (var item in analyticsResponse.All.DailyMetrics)
        {
            if (item.Metrics?.PinClickRate != null)
                result[0].Data.Add(new AnalyticsDataPoint { Date = item.Date, Total = item.Metrics.PinClickRate.ToString() });

            if (item.Metrics?.Impression != null)
                result[1].Data.Add(new AnalyticsDataPoint { Date = item.Date, Total = item.Metrics.Impression.ToString() });

            if (item.Metrics?.PinClick != null)
                result[2].Data.Add(new AnalyticsDataPoint { Date = item.Date, Total = item.Metrics.PinClick.ToString() });

            if (item.Metrics?.Engagement != null)
                result[3].Data.Add(new AnalyticsDataPoint { Date = item.Date, Total = item.Metrics.Engagement.ToString() });

            if (item.Metrics?.Save != null)
                result[4].Data.Add(new AnalyticsDataPoint { Date = item.Date, Total = item.Metrics.Save.ToString() });
        }

        return result.ToArray();
    }

    public override async Task<AnalyticsData[]?> PostAnalyticsAsync(
        string integrationId,
        string accessToken,
        string postId,
        int days,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var since = DateTime.UtcNow.AddYears(-2).ToString("yyyy-MM-dd");

        var url = $"https://api.pinterest.com/v5/pins/{postId}/analytics?start_date={since}&end_date={today}&metric_types=IMPRESSION,PIN_CLICK,OUTBOUND_CLICK,SAVE";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        request.Headers.TryAddWithoutValidation("Content-Type", "application/json");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var analyticsResponse = await DeserializeAsync<PinterestPinAnalyticsResponse>(response);

        if (analyticsResponse.All?.LifetimeMetrics == null)
            return Array.Empty<AnalyticsData>();

        var result = new List<AnalyticsData>();

        if (analyticsResponse.All.LifetimeMetrics.Impression.HasValue)
        {
            result.Add(new AnalyticsData
            {
                Label = "Impressions",
                PercentageChange = 0,
                Data = new List<AnalyticsDataPoint>
                {
                    new() { Total = analyticsResponse.All.LifetimeMetrics.Impression.Value.ToString(), Date = today }
                }
            });
        }

        if (analyticsResponse.All.LifetimeMetrics.PinClick.HasValue)
        {
            result.Add(new AnalyticsData
            {
                Label = "Pin Clicks",
                PercentageChange = 0,
                Data = new List<AnalyticsDataPoint>
                {
                    new() { Total = analyticsResponse.All.LifetimeMetrics.PinClick.Value.ToString(), Date = today }
                }
            });
        }

        if (analyticsResponse.All.LifetimeMetrics.OutboundClick.HasValue)
        {
            result.Add(new AnalyticsData
            {
                Label = "Outbound Clicks",
                PercentageChange = 0,
                Data = new List<AnalyticsDataPoint>
                {
                    new() { Total = analyticsResponse.All.LifetimeMetrics.OutboundClick.Value.ToString(), Date = today }
                }
            });
        }

        if (analyticsResponse.All.LifetimeMetrics.Save.HasValue)
        {
            result.Add(new AnalyticsData
            {
                Label = "Saves",
                PercentageChange = 0,
                Data = new List<AnalyticsDataPoint>
                {
                    new() { Total = analyticsResponse.All.LifetimeMetrics.Save.Value.ToString(), Date = today }
                }
            });
        }

        return result.ToArray();
    }

    public async Task<List<PinterestBoard>> GetBoardsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var url = "https://api.pinterest.com/v5/boards";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var boardsResponse = await DeserializeAsync<PinterestBoardsResponse>(response);
        return boardsResponse.Items ?? new List<PinterestBoard>();
    }

    private async Task<string> UploadVideoAsync(string accessToken, string videoUrl, CancellationToken cancellationToken)
    {
        var mediaUrl = "https://api.pinterest.com/v5/media";

        var payload = new { media_type = "video" };
        var request = CreateJsonRequest(mediaUrl, HttpMethod.Post, payload, accessToken);

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var mediaResponse = await DeserializeAsync<PinterestMediaUploadResponse>(response);

        var videoBytes = await HttpClient.GetByteArrayAsync(videoUrl, cancellationToken);

        var uploadForm = new MultipartFormDataContent();
        if (mediaResponse.UploadParameters != null)
        {
            foreach (var param in mediaResponse.UploadParameters)
            {
                if (!string.IsNullOrEmpty(param.Key) && param.Value != null)
                {
                    uploadForm.Add(new StringContent(param.Value.ToString() ?? ""), param.Key);
                }
            }
        }
        uploadForm.Add(new ByteArrayContent(videoBytes), "file", "video.mp4");

        var uploadResponse = await HttpClient.PostAsync(mediaResponse.UploadUrl, uploadForm, cancellationToken);
        uploadResponse.EnsureSuccessStatusCode();

        var status = "";
        while (status != "succeeded")
        {
            await Task.Delay(30000, cancellationToken);

            var statusUrl = $"https://api.pinterest.com/v5/media/{mediaResponse.MediaId}";
            var statusRequest = new HttpRequestMessage(HttpMethod.Get, statusUrl);
            statusRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

            var statusResponse = await HttpClient.SendAsync(statusRequest, cancellationToken);
            statusResponse.EnsureSuccessStatusCode();

            var statusResult = await DeserializeAsync<PinterestMediaStatusResponse>(statusResponse);
            status = statusResult.Status ?? "";
        }

        return mediaResponse.MediaId;
    }

    private async Task<PinterestUserInfo> FetchUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var url = "https://api.pinterest.com/v5/user_account";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<PinterestUserInfo>(response);
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

    private string GetClientId() => _configuration["PINTEREST_CLIENT_ID"] ?? throw new InvalidOperationException("PINTEREST_CLIENT_ID not configured");
    private string GetClientSecret() => _configuration["PINTEREST_CLIENT_SECRET"] ?? throw new InvalidOperationException("PINTEREST_CLIENT_SECRET not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    private class PinterestTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }

    private class PinterestUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("profile_image")]
        public string? ProfileImage { get; set; }
    }

    private class PinterestPinResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class PinterestBoardsResponse
    {
        [JsonPropertyName("items")]
        public List<PinterestBoard>? Items { get; set; }
    }

    public class PinterestBoard
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private class PinterestMediaUploadResponse
    {
        [JsonPropertyName("upload_url")]
        public string UploadUrl { get; set; } = string.Empty;

        [JsonPropertyName("media_id")]
        public string MediaId { get; set; } = string.Empty;

        [JsonPropertyName("upload_parameters")]
        public Dictionary<string, object?>? UploadParameters { get; set; }
    }

    private class PinterestMediaStatusResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    private class PinterestAnalyticsResponse
    {
        [JsonPropertyName("all")]
        public PinterestAnalyticsAll? All { get; set; }
    }

    private class PinterestAnalyticsAll
    {
        [JsonPropertyName("daily_metrics")]
        public List<PinterestDailyMetric>? DailyMetrics { get; set; }
    }

    private class PinterestDailyMetric
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("metrics")]
        public PinterestMetrics? Metrics { get; set; }
    }

    private class PinterestMetrics
    {
        [JsonPropertyName("PIN_CLICK_RATE")]
        public double? PinClickRate { get; set; }

        [JsonPropertyName("IMPRESSION")]
        public long? Impression { get; set; }

        [JsonPropertyName("PIN_CLICK")]
        public long? PinClick { get; set; }

        [JsonPropertyName("ENGAGEMENT")]
        public long? Engagement { get; set; }

        [JsonPropertyName("SAVE")]
        public long? Save { get; set; }
    }

    private class PinterestPinAnalyticsResponse
    {
        [JsonPropertyName("all")]
        public PinterestPinAnalyticsAll? All { get; set; }
    }

    private class PinterestPinAnalyticsAll
    {
        [JsonPropertyName("lifetime_metrics")]
        public PinterestLifetimeMetrics? LifetimeMetrics { get; set; }
    }

    private class PinterestLifetimeMetrics
    {
        [JsonPropertyName("IMPRESSION")]
        public long? Impression { get; set; }

        [JsonPropertyName("PIN_CLICK")]
        public long? PinClick { get; set; }

        [JsonPropertyName("OUTBOUND_CLICK")]
        public long? OutboundClick { get; set; }

        [JsonPropertyName("SAVE")]
        public long? Save { get; set; }
    }

    #endregion
}
