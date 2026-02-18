using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class HashnodeProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;
    private const string GraphQLEndpoint = "https://gql.hashnode.com";

    public override string Identifier => "hashnode";
    public override string Name => "Hashnode";
    public override string[] Scopes => Array.Empty<string>();
    public override int MaxConcurrentJobs => 3;
    public override EditorType Editor => EditorType.Markdown;

    public HashnodeProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<HashnodeProvider> logger)
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
        var authBody = JsonSerializer.Deserialize<HashnodeAuthBody>(bodyJson)
            ?? throw new BadBodyException(Identifier, "Invalid auth body");

        try
        {
            var query = @"
                query {
                    me {
                        name
                        id
                        profilePicture
                        username
                    }
                }";

            var result = await ExecuteGraphQLAsync<HashnodeMeResponse>(query, null, authBody.ApiKey, cancellationToken);

            return new AuthTokenDetails
            {
                RefreshToken = "",
                ExpiresIn = (int)TimeSpan.FromDays(100).TotalSeconds,
                AccessToken = authBody.ApiKey,
                Id = result.Me.Id ?? "",
                Name = result.Me.Name ?? "",
                Picture = result.Me.ProfilePicture ?? string.Empty,
                Username = result.Me.Username ?? ""
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
        var publication = GetSettingValue<string>(settings, "publication") ?? "";
        var canonical = GetSettingValue<string>(settings, "canonical");
        var tags = GetSettingValue<List<HashnodeTag>>(settings, "tags") ?? new List<HashnodeTag>();
        var subtitle = GetSettingValue<string>(settings, "subtitle");
        var mainImage = GetSettingValue<MediaContent>(settings, "main_image");

        var inputObj = new Dictionary<string, object?>
        {
            ["title"] = title,
            ["publicationId"] = publication,
            ["contentMarkdown"] = firstPost.Message
        };

        if (!string.IsNullOrEmpty(canonical))
        {
            inputObj["originalArticleURL"] = canonical;
        }

        if (tags.Count > 0)
        {
            inputObj["tags"] = tags.Select(t => new { id = t.Value }).ToArray();
        }

        if (!string.IsNullOrEmpty(subtitle))
        {
            inputObj["subtitle"] = subtitle;
        }

        if (mainImage?.Path != null)
        {
            inputObj["coverImageOptions"] = new { coverImageURL = mainImage.Path };
        }

        var mutation = @"
            mutation PublishPost($input: PublishPostInput!) {
                publishPost(input: $input) {
                    post {
                        id
                        url
                    }
                }
            }";

        var variables = new { input = inputObj };

        var result = await ExecuteGraphQLAsync<HashnodePublishResponse>(mutation, variables, accessToken, cancellationToken);

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                Status = "completed",
                PostId = result.PublishPost.Post.Id ?? "",
                ReleaseUrl = result.PublishPost.Post.Url ?? ""
            }
        };
    }

    public async Task<List<HashnodePublication>> GetPublicationsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var query = @"
            query {
                me {
                    publications(first: 50) {
                        edges {
                            node {
                                id
                                title
                            }
                        }
                    }
                }
            }";

        var result = await ExecuteGraphQLAsync<HashnodePublicationsResponse>(query, null, accessToken, cancellationToken);

        return result.Me.Publications.Edges
            .Select(e => new HashnodePublication { Id = e.Node.Id ?? "", Name = e.Node.Title ?? "" })
            .ToList();
    }

    private async Task<T> ExecuteGraphQLAsync<T>(
        string query,
        object? variables,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var payload = new { query, variables };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, GraphQLEndpoint) { Content = content };
        request.Headers.TryAddWithoutValidation("Authorization", accessToken);

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var wrapper = await DeserializeAsync<GraphQLResponse<T>>(response);
        return wrapper.Data;
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

    private class HashnodeAuthBody
    {
        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = string.Empty;
    }

    private class GraphQLResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; } = default!;
    }

    private class HashnodeMeResponse
    {
        [JsonPropertyName("me")]
        public HashnodeMe Me { get; set; } = new();
    }

    private class HashnodeMe
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("profilePicture")]
        public string? ProfilePicture { get; set; }
    }

    private class HashnodePublishResponse
    {
        [JsonPropertyName("publishPost")]
        public HashnodePublishPost PublishPost { get; set; } = new();
    }

    private class HashnodePublishPost
    {
        [JsonPropertyName("post")]
        public HashnodePost Post { get; set; } = new();
    }

    private class HashnodePost
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    private class HashnodePublicationsResponse
    {
        [JsonPropertyName("me")]
        public HashnodeMePublications Me { get; set; } = new();
    }

    private class HashnodeMePublications
    {
        [JsonPropertyName("publications")]
        public HashnodePublications Publications { get; set; } = new();
    }

    private class HashnodePublications
    {
        [JsonPropertyName("edges")]
        public List<HashnodePublicationEdge> Edges { get; set; } = new();
    }

    private class HashnodePublicationEdge
    {
        [JsonPropertyName("node")]
        public HashnodePublicationNode Node { get; set; } = new();
    }

    private class HashnodePublicationNode
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }

    public class HashnodeTag
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class HashnodePublication
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
