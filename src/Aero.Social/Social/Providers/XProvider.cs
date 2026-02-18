using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class XProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "x";
    public override string Name => "X";
    public override string[] Scopes => Array.Empty<string>();
    public override int MaxConcurrentJobs => 1;
    public override string? Tooltip => "You will be logged in into your current account, if you would like a different account, change it first on X";

    public XProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<XProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null)
    {
        var isPremium = additionalSettings is bool premium && premium;
        return isPremium ? 4000 : 200;
    }

    protected override ErrorHandlingResult? HandleErrors(string responseBody)
    {
        if (responseBody.Contains("Unsupported Authentication"))
            return new ErrorHandlingResult(ErrorHandlingType.RefreshToken, "X authentication has expired, please reconnect your account");

        if (responseBody.Contains("usage-capped"))
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "Posting failed - capped reached. Please try again later");

        if (responseBody.Contains("duplicate-rules"))
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "You have already posted this post, please wait before posting again");

        if (responseBody.Contains("The Tweet contains an invalid URL"))
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "The Tweet contains a URL that is not allowed on X");

        if (responseBody.Contains("This user is not allowed to post a video longer than 2 minutes"))
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "The video you are trying to post is longer than 2 minutes, which is not allowed for this account");

        return null;
    }

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(6);
        var apiKey = GetApiKey();
        var apiSecret = GetApiSecret();
        var frontendUrl = GetFrontendUrl();

        var callbackUrl = $"{frontendUrl}/integrations/social/x";

        var requestToken = await GetRequestTokenAsync(apiKey, apiSecret, callbackUrl, cancellationToken);

        var url = $"https://api.twitter.com/oauth/authenticate?oauth_token={requestToken.Token}";

        return new GenerateAuthUrlResponse
        {
            Url = url,
            CodeVerifier = $"{requestToken.Token}:{requestToken.TokenSecret}",
            State = state
        };
    }

    public override async Task<AuthTokenDetails> AuthenticateAsync(
        AuthenticateParams parameters,
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var apiKey = GetApiKey();
        var apiSecret = GetApiSecret();

        var parts = parameters.CodeVerifier.Split(':');
        var oauthToken = parts[0];
        var oauthTokenSecret = parts[1];

        var accessToken = await GetAccessTokenAsync(apiKey, apiSecret, oauthToken, oauthTokenSecret, parameters.Code, cancellationToken);

        var userInfo = await GetUserInfoAsync(apiKey, apiSecret, accessToken.Token, accessToken.TokenSecret, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.Name,
            AccessToken = $"{accessToken.Token}:{accessToken.TokenSecret}",
            RefreshToken = string.Empty,
            ExpiresIn = 999999999,
            Picture = userInfo.ProfileImageUrl ?? string.Empty,
            Username = userInfo.Username,
            AdditionalSettings = new List<AdditionalSetting>
            {
                new()
                {
                    Title = "Verified",
                    Description = "Is this a verified user? (Premium)",
                    Type = AdditionalSettingType.Checkbox,
                    Value = userInfo.Verified
                }
            }
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
        var firstPost = posts.First();
        var settings = firstPost.Settings ?? new Dictionary<string, object>();

        var apiKey = GetApiKey();
        var apiSecret = GetApiSecret();
        var tokenParts = accessToken.Split(':');
        var token = tokenParts[0];
        var tokenSecret = tokenParts[1];

        var mediaIds = await UploadMediaAsync(apiKey, apiSecret, token, tokenSecret, new List<PostDetails> { firstPost }, cancellationToken);

        var userInfo = await GetUserInfoAsync(apiKey, apiSecret, token, tokenSecret, cancellationToken);

        var tweetData = new Dictionary<string, object>
        {
            ["text"] = firstPost.Message
        };

        var replySettings = GetSettingValue<string>(settings, "who_can_reply_post");
        if (!string.IsNullOrEmpty(replySettings) && replySettings != "everyone")
        {
            tweetData["reply_settings"] = replySettings;
        }

        var community = GetSettingValue<string>(settings, "community");
        if (!string.IsNullOrEmpty(community))
        {
            tweetData["share_with_followers"] = true;
            tweetData["community_id"] = community.Split('/').Last();
        }

        if (mediaIds.Count > 0)
        {
            tweetData["media"] = new { media_ids = mediaIds.ToArray() };
        }

        var request = CreateRequest("https://api.twitter.com/2/tweets", HttpMethod.Post, tweetData);
        AddOAuthHeader(request, apiKey, apiSecret, token, tokenSecret, "POST", "https://api.twitter.com/2/tweets");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tweetResponse = await DeserializeAsync<XTweetResponse>(response);
        var tweetId = tweetResponse.Data.Id;

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = tweetId,
                ReleaseUrl = $"https://twitter.com/{userInfo.Username}/status/{tweetId}",
                Status = "posted"
            }
        };
    }

    private async Task<List<string>> UploadMediaAsync(
        string apiKey,
        string apiSecret,
        string token,
        string tokenSecret,
        List<PostDetails> posts,
        CancellationToken cancellationToken)
    {
        var mediaIds = new List<string>();

        foreach (var post in posts)
        {
            foreach (var media in post.Media ?? new List<MediaContent>())
            {
                var mediaBytes = await ReadOrFetchAsync(media.Path, cancellationToken);
                var isVideo = media.Path.Contains(".mp4", StringComparison.OrdinalIgnoreCase);

                if (isVideo)
                {
                    var mediaId = await UploadVideoAsync(apiKey, apiSecret, token, tokenSecret, mediaBytes, cancellationToken);
                    mediaIds.Add(mediaId);
                }
                else
                {
                    var mediaId = await UploadImageAsync(apiKey, apiSecret, token, tokenSecret, mediaBytes, cancellationToken);
                    mediaIds.Add(mediaId);
                }
            }
        }

        return mediaIds;
    }

    private async Task<string> UploadImageAsync(string apiKey, string apiSecret, string token, string tokenSecret, byte[] imageData, CancellationToken cancellationToken)
    {
        var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(imageData), "media", "image.jpg" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://upload.twitter.com/1.1/media/upload.json")
        {
            Content = content
        };
        AddOAuthHeader(request, apiKey, apiSecret, token, tokenSecret, "POST", "https://upload.twitter.com/1.1/media/upload.json");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var uploadResponse = await DeserializeAsync<XMediaUploadResponse>(response);
        return uploadResponse.MediaIdString;
    }

    private async Task<string> UploadVideoAsync(string apiKey, string apiSecret, string token, string tokenSecret, byte[] videoData, CancellationToken cancellationToken)
    {
        var initContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["command"] = "INIT",
            ["media_type"] = "video/mp4",
            ["total_bytes"] = videoData.Length.ToString()
        });

        var initRequest = new HttpRequestMessage(HttpMethod.Post, "https://upload.twitter.com/1.1/media/upload.json")
        {
            Content = initContent
        };
        AddOAuthHeader(initRequest, apiKey, apiSecret, token, tokenSecret, "POST", "https://upload.twitter.com/1.1/media/upload.json");

        var initResponse = await HttpClient.SendAsync(initRequest, cancellationToken);
        initResponse.EnsureSuccessStatusCode();

        var initResult = await DeserializeAsync<XMediaUploadResponse>(initResponse);
        var mediaId = initResult.MediaIdString;

        var appendContent = new MultipartFormDataContent
        {
            { new StringContent("APPEND"), "command" },
            { new StringContent(mediaId), "media_id" },
            { new StringContent("0"), "segment_index" },
            { new ByteArrayContent(videoData), "media", "video.mp4" }
        };

        var appendRequest = new HttpRequestMessage(HttpMethod.Post, "https://upload.twitter.com/1.1/media/upload.json")
        {
            Content = appendContent
        };
        AddOAuthHeader(appendRequest, apiKey, apiSecret, token, tokenSecret, "POST", "https://upload.twitter.com/1.1/media/upload.json");

        await HttpClient.SendAsync(appendRequest, cancellationToken);

        var finalizeContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["command"] = "FINALIZE",
            ["media_id"] = mediaId
        });

        var finalizeRequest = new HttpRequestMessage(HttpMethod.Post, "https://upload.twitter.com/1.1/media/upload.json")
        {
            Content = finalizeContent
        };
        AddOAuthHeader(finalizeRequest, apiKey, apiSecret, token, tokenSecret, "POST", "https://upload.twitter.com/1.1/media/upload.json");

        await HttpClient.SendAsync(finalizeRequest, cancellationToken);

        return mediaId;
    }

    private async Task<XRequestToken> GetRequestTokenAsync(string apiKey, string apiSecret, string callbackUrl, CancellationToken cancellationToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["oauth_callback"] = callbackUrl
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.twitter.com/oauth/request_token")
        {
            Content = content
        };
        AddOAuthHeader(request, apiKey, apiSecret, null, null, "POST", "https://api.twitter.com/oauth/request_token");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        var pairs = responseText.Split('&')
            .Select(p => p.Split('='))
            .ToDictionary(p => p[0], p => p[1]);

        return new XRequestToken
        {
            Token = pairs["oauth_token"],
            TokenSecret = pairs["oauth_token_secret"]
        };
    }

    private async Task<XAccessToken> GetAccessTokenAsync(string apiKey, string apiSecret, string oauthToken, string oauthTokenSecret, string verifier, CancellationToken cancellationToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["oauth_verifier"] = verifier
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.twitter.com/oauth/access_token")
        {
            Content = content
        };
        AddOAuthHeader(request, apiKey, apiSecret, oauthToken, oauthTokenSecret, "POST", "https://api.twitter.com/oauth/access_token");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        var pairs = responseText.Split('&')
            .Select(p => p.Split('='))
            .ToDictionary(p => p[0], p => p[1]);

        return new XAccessToken
        {
            Token = pairs["oauth_token"],
            TokenSecret = pairs["oauth_token_secret"]
        };
    }

    private async Task<XUserInfo> GetUserInfoAsync(string apiKey, string apiSecret, string token, string tokenSecret, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitter.com/2/users/me?user.fields=username,verified,verified_type,profile_image_url,name");
        AddOAuthHeader(request, apiKey, apiSecret, token, tokenSecret, "GET", "https://api.twitter.com/2/users/me");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var userResponse = await DeserializeAsync<XUserResponse>(response);
        return userResponse.Data;
    }

    private void AddOAuthHeader(HttpRequestMessage request, string apiKey, string apiSecret, string? token, string? tokenSecret, string method, string url)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");

        var parameters = new Dictionary<string, string>
        {
            ["oauth_consumer_key"] = apiKey,
            ["oauth_nonce"] = nonce,
            ["oauth_signature_method"] = "HMAC-SHA1",
            ["oauth_timestamp"] = timestamp,
            ["oauth_version"] = "1.0"
        };

        if (!string.IsNullOrEmpty(token))
        {
            parameters["oauth_token"] = token;
        }

        var signature = GenerateOAuthSignature(method, url, parameters, apiSecret, tokenSecret);
        parameters["oauth_signature"] = signature;

        var authHeader = "OAuth " + string.Join(", ", parameters.Select(p => $"{p.Key}=\"{Uri.EscapeDataString(p.Value)}\""));
        request.Headers.Add("Authorization", authHeader);
    }

    private static string GenerateOAuthSignature(string method, string url, Dictionary<string, string> parameters, string apiSecret, string? tokenSecret)
    {
        var baseString = $"{method}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(string.Join("&", parameters.OrderBy(p => p.Key).Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}")))}";
        var signingKey = $"{Uri.EscapeDataString(apiSecret)}&{(tokenSecret != null ? Uri.EscapeDataString(tokenSecret) : "")}";

        using var hmac = new System.Security.Cryptography.HMACSHA1(Encoding.ASCII.GetBytes(signingKey));
        var hash = hmac.ComputeHash(Encoding.ASCII.GetBytes(baseString));
        return Convert.ToBase64String(hash);
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

    private string GetApiKey() => _configuration["X_API_KEY"] ?? throw new InvalidOperationException("X_API_KEY not configured");
    private string GetApiSecret() => _configuration["X_API_SECRET"] ?? throw new InvalidOperationException("X_API_SECRET not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    private class XRequestToken
    {
        public string Token { get; set; } = string.Empty;
        public string TokenSecret { get; set; } = string.Empty;
    }

    private class XAccessToken
    {
        public string Token { get; set; } = string.Empty;
        public string TokenSecret { get; set; } = string.Empty;
    }

    private class XUserResponse
    {
        [JsonPropertyName("data")]
        public XUserInfo Data { get; set; } = new();
    }

    private class XUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("profile_image_url")]
        public string? ProfileImageUrl { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }
    }

    private class XTweetResponse
    {
        [JsonPropertyName("data")]
        public XTweetData Data { get; set; } = new();
    }

    private class XTweetData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class XMediaUploadResponse
    {
        [JsonPropertyName("media_id_string")]
        public string MediaIdString { get; set; } = string.Empty;
    }

    #endregion
}
