using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class LemmyProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "lemmy";
    public override string Name => "Lemmy";
    public override string[] Scopes => Array.Empty<string>();
    public override int MaxConcurrentJobs => 3;

    public LemmyProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LemmyProvider> logger)
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
        var authBody = JsonSerializer.Deserialize<LemmyAuthBody>(bodyJson)
            ?? throw new BadBodyException(Identifier, "Invalid auth body");

        var loginUrl = $"{authBody.Service}/api/v3/user/login";

        var payload = new
        {
            username_or_email = authBody.Identifier,
            password = authBody.Password
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync(loginUrl, content, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new BadBodyException(Identifier, "Invalid credentials");
        }

        response.EnsureSuccessStatusCode();

        var loginResult = await DeserializeAsync<LemmyLoginResponse>(response);

        try
        {
            var userUrl = $"{authBody.Service}/api/v3/user?username={authBody.Identifier}";

            var userRequest = new HttpRequestMessage(HttpMethod.Get, userUrl);
            userRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {loginResult.Jwt}");

            var userResponse = await HttpClient.SendAsync(userRequest, cancellationToken);
            userResponse.EnsureSuccessStatusCode();

            var userResult = await DeserializeAsync<LemmyUserResponse>(userResponse);

            return new AuthTokenDetails
            {
                RefreshToken = loginResult.Jwt!,
                ExpiresIn = (int)TimeSpan.FromDays(100).TotalSeconds,
                AccessToken = loginResult.Jwt!,
                Id = userResult.PersonView.Person.Id.ToString(),
                Name = userResult.PersonView.Person.DisplayName
                       ?? userResult.PersonView.Person.Name
                       ?? "",
                Picture = userResult.PersonView.Person.Avatar ?? string.Empty,
                Username = authBody.Identifier
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
        if (posts.Count == 0)
            return Array.Empty<PostResponse>();

        var authBody = GetAuthBody(integration);
        var jwt = await GetJwtAsync(authBody, cancellationToken);
        var firstPost = posts[0];

        var settings = firstPost.Settings ?? new Dictionary<string, object>();
        var subreddits = GetSettingValue<List<LemmySubreddit>>(settings, "subreddit") ?? new List<LemmySubreddit>();

        var valueArray = new List<PostResponse>();

        foreach (var lemmy in subreddits)
        {
            var payload = new Dictionary<string, object?>
            {
                ["community_id"] = lemmy.Value.Id,
                ["name"] = lemmy.Value.Title,
                ["body"] = firstPost.Message,
                ["nsfw"] = false
            };

            if (!string.IsNullOrEmpty(lemmy.Value.Url))
            {
                var url = lemmy.Value.Url;
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    url = $"https://{url}";
                }
                payload["url"] = url;
            }

            if (firstPost.Media != null && firstPost.Media.Count > 0)
            {
                payload["custom_thumbnail"] = firstPost.Media[0].Path;
            }

            var postUrl = $"{authBody.Service}/api/v3/post";
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, postUrl) { Content = content };
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {jwt}");

            var response = await HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var postResult = await DeserializeAsync<LemmyPostResponse>(response);

            valueArray.Add(new PostResponse
            {
                PostId = postResult.PostView.Post.Id.ToString(),
                ReleaseUrl = $"{authBody.Service}/post/{postResult.PostView.Post.Id}",
                Id = firstPost.Id,
                Status = "published"
            });
        }

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = string.Join(",", valueArray.Select(p => p.PostId)),
                ReleaseUrl = string.Join(",", valueArray.Select(p => p.ReleaseUrl)),
                Status = "published"
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

        var authBody = GetAuthBody(integration);
        var jwt = await GetJwtAsync(authBody, cancellationToken);
        var commentPost = posts[0];

        var postIds = postId.Split(',');
        var valueArray = new List<PostResponse>();

        foreach (var singlePostId in postIds)
        {
            var payload = new
            {
                post_id = int.Parse(singlePostId),
                content = commentPost.Message
            };

            var commentUrl = $"{authBody.Service}/api/v3/comment";
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, commentUrl) { Content = content };
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {jwt}");

            var response = await HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var commentResult = await DeserializeAsync<LemmyCommentResponse>(response);

            valueArray.Add(new PostResponse
            {
                PostId = commentResult.CommentView.Comment.Id.ToString(),
                ReleaseUrl = $"{authBody.Service}/comment/{commentResult.CommentView.Comment.Id}",
                Id = commentPost.Id,
                Status = "published"
            });
        }

        return new[]
        {
            new PostResponse
            {
                Id = commentPost.Id,
                PostId = string.Join(",", valueArray.Select(p => p.PostId)),
                ReleaseUrl = string.Join(",", valueArray.Select(p => p.ReleaseUrl)),
                Status = "published"
            }
        };
    }

    public async Task<List<LemmyCommunity>> SearchCommunitiesAsync(
        Integration integration,
        string query,
        CancellationToken cancellationToken = default)
    {
        var authBody = GetAuthBody(integration);
        var jwt = await GetJwtAsync(authBody, cancellationToken);

        var url = $"{authBody.Service}/api/v3/search?type_=Communities&sort=Active&q={Uri.EscapeDataString(query)}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {jwt}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var searchResult = await DeserializeAsync<LemmySearchResponse>(response);

        return searchResult.Communities?.Select(c => new LemmyCommunity
        {
            Title = c.Community.Title,
            Name = c.Community.Name,
            Id = c.Community.Id
        }).ToList() ?? new List<LemmyCommunity>();
    }

    private async Task<string> GetJwtAsync(LemmyAuthBody authBody, CancellationToken cancellationToken)
    {
        var loginUrl = $"{authBody.Service}/api/v3/user/login";

        var payload = new
        {
            username_or_email = authBody.Identifier,
            password = authBody.Password
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync(loginUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var loginResult = await DeserializeAsync<LemmyLoginResponse>(response);
        return loginResult.Jwt!;
    }

    private static LemmyAuthBody GetAuthBody(Integration integration)
    {
        if (string.IsNullOrEmpty(integration.CustomInstanceDetails))
        {
            throw new InvalidOperationException("No custom instance details for Lemmy");
        }

        var jsonBytes = Convert.FromBase64String(integration.CustomInstanceDetails);
        var json = Encoding.UTF8.GetString(jsonBytes);
        return JsonSerializer.Deserialize<LemmyAuthBody>(json)
            ?? throw new InvalidOperationException("Invalid auth body");
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

    private class LemmyAuthBody
    {
        [JsonPropertyName("service")]
        public string Service { get; set; } = "https://lemmy.world";

        [JsonPropertyName("identifier")]
        public string Identifier { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    private class LemmyLoginResponse
    {
        [JsonPropertyName("jwt")]
        public string? Jwt { get; set; }
    }

    private class LemmyUserResponse
    {
        [JsonPropertyName("person_view")]
        public LemmyPersonView PersonView { get; set; } = new();
    }

    private class LemmyPersonView
    {
        [JsonPropertyName("person")]
        public LemmyPerson Person { get; set; } = new();
    }

    private class LemmyPerson
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }
    }

    private class LemmyPostResponse
    {
        [JsonPropertyName("post_view")]
        public LemmyPostView PostView { get; set; } = new();
    }

    private class LemmyPostView
    {
        [JsonPropertyName("post")]
        public LemmyPost Post { get; set; } = new();
    }

    private class LemmyPost
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class LemmyCommentResponse
    {
        [JsonPropertyName("comment_view")]
        public LemmyCommentView CommentView { get; set; } = new();
    }

    private class LemmyCommentView
    {
        [JsonPropertyName("comment")]
        public LemmyComment Comment { get; set; } = new();
    }

    private class LemmyComment
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    private class LemmySearchResponse
    {
        [JsonPropertyName("communities")]
        public List<LemmyCommunityView>? Communities { get; set; }
    }

    private class LemmyCommunityView
    {
        [JsonPropertyName("community")]
        public LemmyCommunityDetail Community { get; set; } = new();
    }

    private class LemmyCommunityDetail
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
    }

    public class LemmyCommunity
    {
        public string Title { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Id { get; set; }
    }

    public class LemmySubreddit
    {
        public LemmySubredditValue Value { get; set; } = new();
    }

    public class LemmySubredditValue
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
    }

    #endregion
}
