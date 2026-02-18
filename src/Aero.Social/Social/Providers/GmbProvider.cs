using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class GmbProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "gmb";
    public override string Name => "Google My Business";
    public override string[] Scopes => new[]
    {
        "https://www.googleapis.com/auth/userinfo.profile",
        "https://www.googleapis.com/auth/userinfo.email",
        "https://www.googleapis.com/auth/business.manage"
    };
    public override int MaxConcurrentJobs => 3;
    public override bool IsBetweenSteps => true;

    public GmbProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GmbProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 1500;

    protected override ErrorHandlingResult? HandleErrors(string responseBody)
    {
        if (responseBody.Contains("UNAUTHENTICATED") || responseBody.Contains("invalid_grant"))
            return new ErrorHandlingResult(ErrorHandlingType.RefreshToken, "Please re-authenticate your Google My Business account");

        if (responseBody.Contains("Unauthorized"))
            return new ErrorHandlingResult(ErrorHandlingType.RefreshToken, "Token expired or invalid, please reconnect your Google My Business account");

        if (responseBody.Contains("PERMISSION_DENIED"))
            return new ErrorHandlingResult(ErrorHandlingType.RefreshToken, "Permission denied. Please ensure you have access to this business location");

        if (responseBody.Contains("NOT_FOUND"))
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "Business location not found. It may have been deleted");

        if (responseBody.Contains("INVALID_ARGUMENT"))
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "Invalid post content. Please check your post details");

        if (responseBody.Contains("RESOURCE_EXHAUSTED"))
            return new ErrorHandlingResult(ErrorHandlingType.BadBody, "Rate limit exceeded. Please try again later");

        return null;
    }

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(7);
        var clientId = GetClientId();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = $"{frontendUrl}/integrations/social/gmb";

        var url = "https://accounts.google.com/o/oauth2/v2/auth" +
                  $"?access_type=offline" +
                  $"&prompt=consent" +
                  $"&state={state}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&response_type=code" +
                  $"&client_id={clientId}" +
                  $"&scope={Uri.EscapeDataString(string.Join(" ", Scopes))}";

        return new GenerateAuthUrlResponse
        {
            Url = url,
            CodeVerifier = MakeId(11),
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
        var redirectUri = $"{frontendUrl}/integrations/social/gmb";

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = parameters.Code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri
        };

        var response = await HttpClient.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(form), cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenInfo = await DeserializeAsync<GoogleTokenResponse>(response);
        var userInfo = await GetUserInfoAsync(tokenInfo.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.Name,
            AccessToken = tokenInfo.AccessToken,
            RefreshToken = tokenInfo.RefreshToken ?? "",
            ExpiresIn = tokenInfo.ExpiresIn,
            Picture = userInfo.Picture ?? string.Empty,
            Username = ""
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
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        };

        var response = await HttpClient.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(form), cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenInfo = await DeserializeAsync<GoogleTokenResponse>(response);
        var userInfo = await GetUserInfoAsync(tokenInfo.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.Id,
            Name = userInfo.Name,
            AccessToken = tokenInfo.AccessToken,
            RefreshToken = tokenInfo.RefreshToken ?? refreshToken,
            ExpiresIn = tokenInfo.ExpiresIn,
            Picture = userInfo.Picture ?? string.Empty,
            Username = ""
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

        var topicType = GetSettingValue<string>(settings, "topicType") ?? "STANDARD";
        var callToActionType = GetSettingValue<string>(settings, "callToActionType");
        var callToActionUrl = GetSettingValue<string>(settings, "callToActionUrl");

        var postBody = new Dictionary<string, object?>
        {
            ["languageCode"] = "en",
            ["summary"] = firstPost.Message,
            ["topicType"] = topicType
        };

        if (!string.IsNullOrEmpty(callToActionType) && callToActionType != "NONE" && !string.IsNullOrEmpty(callToActionUrl))
        {
            postBody["callToAction"] = new
            {
                actionType = callToActionType,
                url = callToActionUrl
            };
        }

        if (firstPost.Media != null && firstPost.Media.Count > 0)
        {
            var mediaItem = firstPost.Media[0];
            postBody["media"] = new[]
            {
                new
                {
                    mediaFormat = mediaItem.Path.Contains(".mp4") ? "VIDEO" : "PHOTO",
                    sourceUrl = mediaItem.Path
                }
            };
        }

        var json = JsonSerializer.Serialize(postBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"https://mybusiness.googleapis.com/v4/{id}/localPosts") { Content = content };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var postResponse = await DeserializeAsync<GmbPostResponse>(response);
        var locationId = id.Split('/').Last();

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = postResponse.Name ?? "",
                ReleaseUrl = $"https://business.google.com/locations/{locationId}",
                Status = "success"
            }
        };
    }

    public override async Task<AuthTokenDetails?> ReConnectAsync(
        string id,
        string requiredId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var pages = await GetPagesAsync(accessToken, cancellationToken);
        var page = pages.FirstOrDefault(p => p.Id == requiredId);

        if (page == null)
            throw new BadBodyException(Identifier, "Location not found");

        var info = await FetchPageInformationAsync(accessToken, page, cancellationToken);

        return new AuthTokenDetails
        {
            Id = info.Id,
            Name = info.Name,
            AccessToken = accessToken,
            Picture = info.Picture ?? string.Empty,
            Username = ""
        };
    }

    public override async Task<FetchPageInformationResult?> FetchPageInformationAsync(
        string accessToken,
        object data,
        CancellationToken cancellationToken = default)
    {
        var pageData = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(data));
        var locationName = pageData?.GetValueOrDefault("locationName")?.ToString() ?? "";

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://mybusinessbusinessinformation.googleapis.com/v1/{locationName}?readMask=name,title,storefrontAddress,metadata");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var locationData = await DeserializeAsync<GmbLocationResponse>(response);

        return new FetchPageInformationResult
        {
            Id = pageData?.GetValueOrDefault("id")?.ToString() ?? "",
            Name = locationData.Title ?? "",
            AccessToken = accessToken,
            Picture = "",
            Username = ""
        };
    }

    public async Task<List<GmbPage>> GetPagesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://mybusinessaccountmanagement.googleapis.com/v1/accounts");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var accountsData = await DeserializeAsync<GmbAccountsResponse>(response);
        var allLocations = new List<GmbPage>();

        if (accountsData.Accounts == null)
            return allLocations;

        foreach (var account in accountsData.Accounts)
        {
            try
            {
                var locationsRequest = new HttpRequestMessage(HttpMethod.Get, $"https://mybusinessbusinessinformation.googleapis.com/v1/{account.Name}/locations?readMask=name,title,storefrontAddress,metadata");
                locationsRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

                var locationsResponse = await HttpClient.SendAsync(locationsRequest, cancellationToken);
                if (!locationsResponse.IsSuccessStatusCode) continue;

                var locationsData = await DeserializeAsync<GmbLocationsResponse>(locationsResponse);

                if (locationsData.Locations != null)
                {
                    foreach (var location in locationsData.Locations)
                    {
                        var locationId = location.Name?.Replace("locations/", "") ?? "";
                        allLocations.Add(new GmbPage
                        {
                            Id = $"{account.Name}/locations/{locationId}",
                            Name = location.Title ?? "Unnamed Location",
                            AccountName = account.Name ?? "",
                            LocationName = location.Name ?? ""
                        });
                    }
                }
            }
            catch
            {
                // Continue with other accounts
            }
        }

        return allLocations;
    }

    private async Task<GmbUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<GmbUserInfo>(response);
    }

    private async Task<GmbPageInfo> FetchPageInformationAsync(string accessToken, GmbPage page, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://mybusinessbusinessinformation.googleapis.com/v1/{page.LocationName}?readMask=name,title");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var locationData = await DeserializeAsync<GmbLocationResponse>(response);

        return new GmbPageInfo
        {
            Id = page.Id,
            Name = locationData.Title ?? "",
            AccessToken = accessToken,
            Picture = ""
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

    private string GetClientId() => _configuration["GOOGLE_GMB_CLIENT_ID"] ?? _configuration["YOUTUBE_CLIENT_ID"] ?? throw new InvalidOperationException("GOOGLE_GMB_CLIENT_ID not configured");
    private string GetClientSecret() => _configuration["GOOGLE_GMB_CLIENT_SECRET"] ?? _configuration["YOUTUBE_CLIENT_SECRET"] ?? throw new InvalidOperationException("GOOGLE_GMB_CLIENT_SECRET not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    private class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private class GmbUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }

    private class GmbPostResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class GmbAccountsResponse
    {
        [JsonPropertyName("accounts")]
        public List<GmbAccount>? Accounts { get; set; }
    }

    private class GmbAccount
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class GmbLocationsResponse
    {
        [JsonPropertyName("locations")]
        public List<GmbLocation>? Locations { get; set; }
    }

    private class GmbLocation
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }

    private class GmbLocationResponse
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }

    public class GmbPage
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
    }

    private class GmbPageInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string? Picture { get; set; }
    }

    #endregion
}
