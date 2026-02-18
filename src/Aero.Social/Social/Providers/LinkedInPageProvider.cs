using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Plugs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class LinkedInPageProvider : LinkedInProvider
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "linkedin-page";
    public override string Name => "LinkedIn Page";
    public override bool IsBetweenSteps => true;
    public override bool RefreshWait => true;
    public override int MaxConcurrentJobs => 2;
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

    public LinkedInPageProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LinkedInPageProvider> logger)
        : base(httpClient, configuration, logger)
    {
        _configuration = configuration;
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
                  $"&prompt=none" +
                  $"&client_id={clientId}" +
                  $"&redirect_uri={Uri.EscapeDataString($"{frontendUrl}/integrations/social/linkedin-page")}" +
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
        var redirectUri = $"{frontendUrl}/integrations/social/linkedin-page";

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
            Id = $"p_{userInfo.Sub}",
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
        return await PostAsCompanyAsync(id, accessToken, posts, integration, cancellationToken);
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
        return await CommentAsCompanyAsync(id, postId, accessToken, posts, integration, cancellationToken);
    }

    public async Task<List<LinkedInCompany>> GetCompaniesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var url = "https://api.linkedin.com/v2/organizationalEntityAcls?q=roleAssignee&role=ADMINISTRATOR&projection=(elements*(organizationalTarget~(localizedName,vanityName,logoV2(original~:playableStreams))))";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
        request.Headers.Add("LinkedIn-Version", "202501");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var companiesResponse = await DeserializeAsync<LinkedInCompaniesResponse>(response);

        return (companiesResponse.Elements ?? [])
            .Select(e => new LinkedInCompany
            {
                Id = e.OrganizationalTarget?.Split(':').Last() ?? string.Empty,
                Page = e.OrganizationalTarget?.Split(':').Last() ?? string.Empty,
                Username = e.OrganizationalTargetDetails?.VanityName ?? string.Empty,
                Name = e.OrganizationalTargetDetails?.LocalizedName ?? string.Empty,
                Picture = e.OrganizationalTargetDetails?.LogoV2?.Original?.Elements?.FirstOrDefault()?.Identifiers?.FirstOrDefault()?.Identifier ?? string.Empty
            })
            .ToList();
    }

    public override async Task<AuthTokenDetails?> ReConnectAsync(
        string id,
        string requiredId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var pageInformation = await FetchPageInformationAsync(accessToken, new { page = requiredId }, cancellationToken);
        if (pageInformation == null) return null;

        return new AuthTokenDetails
        {
            Id = pageInformation.Id,
            Name = pageInformation.Name,
            AccessToken = pageInformation.AccessToken,
            Picture = pageInformation.Picture,
            Username = pageInformation.Username
        };
    }

    public override async Task<FetchPageInformationResult?> FetchPageInformationAsync(
        string accessToken,
        object data,
        CancellationToken cancellationToken = default)
    {
        var pageId = data.GetType().GetProperty("page")?.GetValue(data)?.ToString();
        if (string.IsNullOrEmpty(pageId)) return null;

        var url = $"https://api.linkedin.com/v2/organizations/{pageId}?projection=(id,localizedName,vanityName,logoV2(original~:playableStreams))";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var orgResponse = await DeserializeAsync<LinkedInOrganizationResponse>(response);

        return new FetchPageInformationResult
        {
            Id = orgResponse.Id,
            Name = orgResponse.LocalizedName,
            AccessToken = accessToken,
            Picture = orgResponse.LogoV2?.Original?.Elements?.FirstOrDefault()?.Identifiers?.FirstOrDefault()?.Identifier ?? string.Empty,
            Username = orgResponse.VanityName ?? string.Empty
        };
    }

    public override async Task<AnalyticsData[]?> AnalyticsAsync(
        string id,
        string accessToken,
        int days,
        CancellationToken cancellationToken = default)
    {
        var endDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var startDate = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeMilliseconds();

        var analytics = new Dictionary<string, List<AnalyticsDataPoint>>
        {
            ["Page Views"] = [],
            ["Clicks"] = [],
            ["Shares"] = [],
            ["Engagement"] = [],
            ["Comments"] = [],
            ["Organic Followers"] = [],
            ["Paid Followers"] = []
        };

        var pageStatsUrl = $"https://api.linkedin.com/v2/organizationPageStatistics?q=organization&organization={Uri.EscapeDataString($"urn:li:organization:{id}")}&timeIntervals=(timeRange:(start:{startDate},end:{endDate}),timeGranularityType:DAY)";
        await FetchAnalyticsAsync(pageStatsUrl, accessToken, analytics, cancellationToken);

        var followerStatsUrl = $"https://api.linkedin.com/v2/organizationalEntityFollowerStatistics?q=organizationalEntity&organizationalEntity={Uri.EscapeDataString($"urn:li:organization:{id}")}&timeIntervals=(timeRange:(start:{startDate},end:{endDate}),timeGranularityType:DAY)";
        await FetchAnalyticsAsync(followerStatsUrl, accessToken, analytics, cancellationToken);

        var shareStatsUrl = $"https://api.linkedin.com/v2/organizationalEntityShareStatistics?q=organizationalEntity&organizationalEntity={Uri.EscapeDataString($"urn:li:organization:{id}")}&timeIntervals=(timeRange:(start:{startDate},end:{endDate}),timeGranularityType:DAY)";
        await FetchAnalyticsAsync(shareStatsUrl, accessToken, analytics, cancellationToken);

        return analytics
            .Where(kvp => kvp.Value.Count > 0)
            .Select(kvp => new AnalyticsData
            {
                Label = kvp.Key,
                Data = kvp.Value,
                PercentageChange = 5
            })
            .ToArray();
    }

    public override async Task<AnalyticsData[]?> PostAnalyticsAsync(
        string integrationId,
        string accessToken,
        string postId,
        int days,
        CancellationToken cancellationToken = default)
    {
        var endDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var startDate = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeMilliseconds();

        var shareStatsUrl = $"https://api.linkedin.com/v2/organizationalEntityShareStatistics?q=organizationalEntity&organizationalEntity={Uri.EscapeDataString($"urn:li:organization:{integrationId}")}&shares=List({Uri.EscapeDataString(postId)})&timeIntervals=(timeRange:(start:{startDate},end:{endDate}),timeGranularityType:DAY)";

        var request = new HttpRequestMessage(HttpMethod.Get, shareStatsUrl);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Headers.Add("LinkedIn-Version", "202511");
        request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var shareStatsResponse = await DeserializeAsync<LinkedInShareStatsResponse>(response);

        var analytics = new Dictionary<string, List<AnalyticsDataPoint>>
        {
            ["Impressions"] = [],
            ["Unique Impressions"] = [],
            ["Clicks"] = [],
            ["Likes"] = [],
            ["Comments"] = [],
            ["Shares"] = [],
            ["Engagement"] = []
        };

        foreach (var element in shareStatsResponse.Elements ?? [])
        {
            if (element.TotalShareStatistics != null)
            {
                var dateStr = DateTimeOffset.FromUnixTimeMilliseconds(element.TimeRange.Start).ToString("yyyy-MM-dd");

                analytics["Impressions"].Add(new AnalyticsDataPoint { Total = element.TotalShareStatistics.ImpressionCount.ToString(), Date = dateStr });
                analytics["Unique Impressions"].Add(new AnalyticsDataPoint { Total = element.TotalShareStatistics.UniqueImpressionsCount.ToString(), Date = dateStr });
                analytics["Clicks"].Add(new AnalyticsDataPoint { Total = element.TotalShareStatistics.ClickCount.ToString(), Date = dateStr });
                analytics["Likes"].Add(new AnalyticsDataPoint { Total = element.TotalShareStatistics.LikeCount.ToString(), Date = dateStr });
                analytics["Comments"].Add(new AnalyticsDataPoint { Total = element.TotalShareStatistics.CommentCount.ToString(), Date = dateStr });
                analytics["Shares"].Add(new AnalyticsDataPoint { Total = element.TotalShareStatistics.ShareCount.ToString(), Date = dateStr });
                analytics["Engagement"].Add(new AnalyticsDataPoint { Total = element.TotalShareStatistics.Engagement.ToString(), Date = dateStr });
            }
        }

        if (analytics.All(kvp => kvp.Value.Count == 0))
        {
            try
            {
                var socialActionsUrl = $"https://api.linkedin.com/v2/socialActions/{Uri.EscapeDataString(postId)}";
                var socialRequest = new HttpRequestMessage(HttpMethod.Get, socialActionsUrl);
                socialRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
                socialRequest.Headers.Add("LinkedIn-Version", "202511");
                socialRequest.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

                var socialResponse = await HttpClient.SendAsync(socialRequest, cancellationToken);
                if (socialResponse.IsSuccessStatusCode)
                {
                    var socialActions = await DeserializeAsync<LinkedInSocialActionsResponse>(socialResponse);
                    var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

                    if (socialActions.LikesSummary != null)
                    {
                        analytics["Likes"].Add(new AnalyticsDataPoint { Total = socialActions.LikesSummary.TotalLikes.ToString(), Date = today });
                    }
                    if (socialActions.CommentsSummary != null)
                    {
                        analytics["Comments"].Add(new AnalyticsDataPoint { Total = socialActions.CommentsSummary.TotalFirstLevelComments.ToString(), Date = today });
                    }
                }
            }
            catch
            {
            }
        }

        return analytics
            .Where(kvp => kvp.Value.Count > 0)
            .Select(kvp => new AnalyticsData
            {
                Label = kvp.Key,
                Data = kvp.Value,
                PercentageChange = 0
            })
            .ToArray();
    }

    private async Task FetchAnalyticsAsync(string url, string accessToken, Dictionary<string, List<AnalyticsDataPoint>> analytics, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Headers.Add("LinkedIn-Version", "202511");
        request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var statsResponse = await DeserializeAsync<LinkedInAnalyticsResponse>(response);

        foreach (var element in statsResponse.Elements ?? [])
        {
            var dateStr = DateTimeOffset.FromUnixTimeMilliseconds(element.TimeRange.Start).ToString("yyyy-MM-dd");

            if (element.TotalPageStatistics?.Views?.AllPageViews != null)
            {
                analytics["Page Views"].Add(new AnalyticsDataPoint
                {
                    Total = element.TotalPageStatistics.Views.AllPageViews.PageViews.ToString(),
                    Date = dateStr
                });
            }

            if (element.FollowerGains != null)
            {
                analytics["Organic Followers"].Add(new AnalyticsDataPoint
                {
                    Total = element.FollowerGains.OrganicFollowerGain.ToString(),
                    Date = dateStr
                });
                analytics["Paid Followers"].Add(new AnalyticsDataPoint
                {
                    Total = element.FollowerGains.PaidFollowerGain.ToString(),
                    Date = dateStr
                });
            }

            if (element.TotalShareStatistics != null)
            {
                analytics["Clicks"].Add(new AnalyticsDataPoint { Total = element.TotalShareStatistics.ClickCount.ToString(), Date = dateStr });
                analytics["Shares"].Add(new AnalyticsDataPoint { Total = element.TotalShareStatistics.ShareCount.ToString(), Date = dateStr });
                analytics["Engagement"].Add(new AnalyticsDataPoint { Total = element.TotalShareStatistics.Engagement.ToString(), Date = dateStr });
                analytics["Comments"].Add(new AnalyticsDataPoint { Total = element.TotalShareStatistics.CommentCount.ToString(), Date = dateStr });
            }
        }
    }

    private async Task<PostResponse[]> PostAsCompanyAsync(
        string id,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken)
    {
        var firstPost = posts.First();
        var settings = firstPost.Settings ?? new Dictionary<string, object>();

        List<string> mediaIds = new();

        if (firstPost.Media != null && firstPost.Media.Count > 0)
        {
            foreach (var media in firstPost.Media)
            {
                var mediaId = await UploadMediaAsCompanyAsync(id, accessToken, media, cancellationToken);
                mediaIds.Add(mediaId);
            }
        }

        var author = $"urn:li:organization:{id}";
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

    private async Task<PostResponse[]?> CommentAsCompanyAsync(
        string id,
        string postId,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken)
    {
        var commentPost = posts.First();
        var actor = $"urn:li:organization:{id}";

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

    private async Task<string> UploadMediaAsCompanyAsync(string companyId, string accessToken, MediaContent media, CancellationToken cancellationToken)
    {
        var isVideo = media.Path.Contains(".mp4", StringComparison.OrdinalIgnoreCase);
        var endpoint = isVideo ? "videos" : "images";

        var mediaBytes = await ReadOrFetchAsync(media.Path, cancellationToken);

        var initializePayload = new
        {
            initializeUploadRequest = new
            {
                owner = $"urn:li:organization:{companyId}",
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

    #region DTOs

    public class LinkedInCompany
    {
        public string Id { get; set; } = string.Empty;
        public string Page { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
    }

    private class LinkedInCompaniesResponse
    {
        [JsonPropertyName("elements")]
        public List<LinkedInCompanyElement>? Elements { get; set; }
    }

    private class LinkedInCompanyElement
    {
        [JsonPropertyName("organizationalTarget")]
        public string? OrganizationalTarget { get; set; }

        [JsonPropertyName("organizationalTarget~")]
        public LinkedInOrganizationalTargetDetails? OrganizationalTargetDetails { get; set; }
    }

    private class LinkedInOrganizationalTargetDetails
    {
        [JsonPropertyName("localizedName")]
        public string? LocalizedName { get; set; }

        [JsonPropertyName("vanityName")]
        public string? VanityName { get; set; }

        [JsonPropertyName("logoV2")]
        public LinkedInLogoV2? LogoV2 { get; set; }
    }

    private class LinkedInLogoV2
    {
        [JsonPropertyName("original~")]
        public LinkedInLogoOriginal? Original { get; set; }
    }

    private class LinkedInLogoOriginal
    {
        [JsonPropertyName("elements")]
        public List<LinkedInLogoElement>? Elements { get; set; }
    }

    private class LinkedInLogoElement
    {
        [JsonPropertyName("identifiers")]
        public List<LinkedInIdentifier>? Identifiers { get; set; }
    }

    private class LinkedInIdentifier
    {
        [JsonPropertyName("identifier")]
        public string? Identifier { get; set; }
    }

    private class LinkedInOrganizationResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("localizedName")]
        public string LocalizedName { get; set; } = string.Empty;

        [JsonPropertyName("vanityName")]
        public string? VanityName { get; set; }

        [JsonPropertyName("logoV2")]
        public LinkedInLogoV2? LogoV2 { get; set; }
    }

    private class LinkedInTokenResponse
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

    private class LinkedInUploadResponse
    {
        [JsonPropertyName("value")]
        public LinkedInUploadValue Value { get; set; } = new();
    }

    private class LinkedInUploadValue
    {
        [JsonPropertyName("uploadUrl")]
        public string UploadUrl { get; set; } = string.Empty;

        [JsonPropertyName("image")]
        public string Image { get; set; } = string.Empty;
    }

    private class LinkedInCommentResponse
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;
    }

    private class LinkedInAnalyticsResponse
    {
        [JsonPropertyName("elements")]
        public List<LinkedInAnalyticsElement>? Elements { get; set; }
    }

    private class LinkedInAnalyticsElement
    {
        [JsonPropertyName("timeRange")]
        public LinkedInTimeRange TimeRange { get; set; } = new();

        [JsonPropertyName("totalPageStatistics")]
        public LinkedInTotalPageStatistics? TotalPageStatistics { get; set; }

        [JsonPropertyName("followerGains")]
        public LinkedInFollowerGains? FollowerGains { get; set; }

        [JsonPropertyName("totalShareStatistics")]
        public LinkedInShareStatistics? TotalShareStatistics { get; set; }
    }

    private class LinkedInTimeRange
    {
        [JsonPropertyName("start")]
        public long Start { get; set; }

        [JsonPropertyName("end")]
        public long End { get; set; }
    }

    private class LinkedInTotalPageStatistics
    {
        [JsonPropertyName("views")]
        public LinkedInViews? Views { get; set; }
    }

    private class LinkedInViews
    {
        [JsonPropertyName("allPageViews")]
        public LinkedInPageViews? AllPageViews { get; set; }
    }

    private class LinkedInPageViews
    {
        [JsonPropertyName("pageViews")]
        public int PageViews { get; set; }
    }

    private class LinkedInFollowerGains
    {
        [JsonPropertyName("organicFollowerGain")]
        public int OrganicFollowerGain { get; set; }

        [JsonPropertyName("paidFollowerGain")]
        public int PaidFollowerGain { get; set; }
    }

    private class LinkedInShareStatistics
    {
        [JsonPropertyName("impressionCount")]
        public int ImpressionCount { get; set; }

        [JsonPropertyName("uniqueImpressionsCount")]
        public int UniqueImpressionsCount { get; set; }

        [JsonPropertyName("clickCount")]
        public int ClickCount { get; set; }

        [JsonPropertyName("likeCount")]
        public int LikeCount { get; set; }

        [JsonPropertyName("commentCount")]
        public int CommentCount { get; set; }

        [JsonPropertyName("shareCount")]
        public int ShareCount { get; set; }

        [JsonPropertyName("engagement")]
        public decimal Engagement { get; set; }
    }

    private class LinkedInShareStatsResponse
    {
        [JsonPropertyName("elements")]
        public List<LinkedInShareStatsElement>? Elements { get; set; }
    }

    private class LinkedInShareStatsElement
    {
        [JsonPropertyName("timeRange")]
        public LinkedInTimeRange TimeRange { get; set; } = new();

        [JsonPropertyName("totalShareStatistics")]
        public LinkedInShareStatistics? TotalShareStatistics { get; set; }
    }

    private class LinkedInSocialActionsResponse
    {
        [JsonPropertyName("likesSummary")]
        public LinkedInLikesSummary? LikesSummary { get; set; }

        [JsonPropertyName("commentsSummary")]
        public LinkedInCommentsSummary? CommentsSummary { get; set; }
    }

    private class LinkedInLikesSummary
    {
        [JsonPropertyName("totalLikes")]
        public int TotalLikes { get; set; }
    }

    private class LinkedInCommentsSummary
    {
        [JsonPropertyName("totalFirstLevelComments")]
        public int TotalFirstLevelComments { get; set; }
    }

    #endregion

    #region Plug Examples

    /// <summary>
    /// Auto-reposts a post when it reaches a certain number of likes
    /// </summary>
    [PostPlug(
        identifier: "auto-repost-post",
        title: "Auto Repost Post",
        description: "Automatically reposts when the post reaches a specified number of likes",
        runEveryMilliseconds: 21600000, // 6 hours
        totalRuns: 10)]
    [PlugField("minLikes", "number", "10", "Minimum number of likes before reposting")]
    [PlugField("message", "string", "Check out this popular post!", "Message to include with the repost")]
    public async Task AutoRepostPost(
        string postId,
        string accessToken,
        int minLikes,
        string message,
        CancellationToken cancellationToken = default)
    {
        // Get current post analytics
        var analytics = await PostAnalyticsAsync(postId, accessToken, postId, 1, cancellationToken);
        var likesMetric = analytics?.FirstOrDefault(a => a.Label == "Likes");
        var currentLikes = likesMetric?.Data.Sum(d => int.Parse(d.Total)) ?? 0;

        if (currentLikes >= minLikes)
        {
            Logger.LogInformation("Post {PostId} has reached {Likes} likes. Reposting...", postId, currentLikes);

            // Get original post content
            var url = $"https://api.linkedin.com/v2/posts/{postId}?projection=(id,author,commentary,content)";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            request.Headers.Add("LinkedIn-Version", "202511");
            request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

            var response = await HttpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var postContent = await DeserializeAsync<LinkedInPostContent>(response);

                // Create repost with the message
                var repostPayload = new
                {
                    author = postContent.Author,
                    commentary = $"{message}\n\n{postContent.Commentary}",
                    visibility = "PUBLIC",
                    distribution = new
                    {
                        feedDistribution = "MAIN_FEED",
                        targetEntities = Array.Empty<string>(),
                        thirdPartyDistributionChannels = Array.Empty<string>()
                    },
                    lifecycleState = "PUBLISHED",
                    isReshareDisabledByAuthor = false,
                    resharedContent = postId
                };

                var repostRequest = CreateRequest("https://api.linkedin.com/rest/posts", HttpMethod.Post, repostPayload);
                repostRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
                repostRequest.Headers.Add("LinkedIn-Version", "202511");
                repostRequest.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

                var repostResponse = await HttpClient.SendAsync(repostRequest, cancellationToken);
                repostResponse.EnsureSuccessStatusCode();

                Logger.LogInformation("Successfully reposted post {PostId}", postId);
            }
        }
        else
        {
            Logger.LogDebug("Post {PostId} has {Likes} likes, waiting for {MinLikes} to repost",
                postId, currentLikes, minLikes);
        }
    }

    /// <summary>
    /// Auto-adds a comment when a post reaches a certain number of likes
    /// </summary>
    [PostPlug(
        identifier: "auto-plug-post",
        title: "Auto Plug Post",
        description: "Automatically adds a promotional comment when the post reaches a specified number of likes",
        runEveryMilliseconds: 21600000, // 6 hours
        totalRuns: 5)]
    [PlugField("minLikes", "number", "50", "Minimum number of likes before adding the comment")]
    [PlugField("comment", "string", "Thanks for the engagement! Follow for more content.", "Comment text to add")]
    public async Task AutoPlugPost(
        string postId,
        string accessToken,
        int minLikes,
        string comment,
        CancellationToken cancellationToken = default)
    {
        // Get current post analytics
        var analytics = await PostAnalyticsAsync(postId, accessToken, postId, 1, cancellationToken);
        var likesMetric = analytics?.FirstOrDefault(a => a.Label == "Likes");
        var currentLikes = likesMetric?.Data.Sum(d => int.Parse(d.Total)) ?? 0;

        if (currentLikes >= minLikes)
        {
            Logger.LogInformation("Post {PostId} has reached {Likes} likes. Adding comment...", postId, currentLikes);

            // Add comment using the existing CommentAsync method
            var commentPosts = new List<PostDetails>
            {
                new() { Message = comment }
            };

            var integration = new Integration { Id = postId };

            await CommentAsync(postId, postId, null, accessToken, commentPosts, integration, cancellationToken);

            Logger.LogInformation("Successfully added comment to post {PostId}", postId);
        }
        else
        {
            Logger.LogDebug("Post {PostId} has {Likes} likes, waiting for {MinLikes} to add comment",
                postId, currentLikes, minLikes);
        }
    }

    private class LinkedInPostContent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        [JsonPropertyName("commentary")]
        public string Commentary { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public object? Content { get; set; }
    }

    #endregion
}
