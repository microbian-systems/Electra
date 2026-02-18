using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class BlueskyProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "bluesky";
    public override string Name => "Bluesky";
    public override string[] Scopes => new[] { "write:statuses", "profile", "write:media" };
    public override int MaxConcurrentJobs => 2;
    public override string? Tooltip => "We don't currently support two-factor authentication. If it's enabled on Bluesky, you'll need to disable it.";

    public BlueskyProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BlueskyProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 300;

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
        var authBody = JsonSerializer.Deserialize<BlueskyAuthBody>(bodyJson)
            ?? throw new BadBodyException(Identifier, "Invalid auth body");

        try
        {
            var session = await LoginAsync(authBody.Service, authBody.Identifier, authBody.Password, cancellationToken);
            var profile = await GetProfileAsync(authBody.Service, session.Did, session.AccessJwt, cancellationToken);

            return new AuthTokenDetails
            {
                RefreshToken = session.RefreshJwt,
                ExpiresIn = (int)TimeSpan.FromDays(100).TotalSeconds,
                AccessToken = session.AccessJwt,
                Id = session.Did,
                Name = profile.DisplayName ?? session.Handle,
                Picture = profile.Avatar ?? string.Empty,
                Username = session.Handle
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
        var session = await LoginAsync(authBody.Service, authBody.Identifier, authBody.Password, cancellationToken);
        var firstPost = posts[0];

        var embed = await UploadMediaForPostAsync(authBody.Service, session, firstPost, cancellationToken);

        var record = new Dictionary<string, object>
        {
            ["$type"] = "app.bsky.feed.post",
            ["text"] = firstPost.Message,
            ["createdAt"] = DateTime.UtcNow.ToString("o")
        };

        var facets = await DetectFacetsAsync(authBody.Service, session, firstPost.Message, cancellationToken);
        if (facets != null && facets.Count > 0)
        {
            record["facets"] = facets;
        }

        if (embed != null)
        {
            record["embed"] = embed;
        }

        var postResult = await CreateRecordAsync(authBody.Service, session, "app.bsky.feed.post", record, cancellationToken);

        var postId = postResult.Uri;
        var postKey = postId.Split('/').Last();

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = postId,
                Status = "completed",
                ReleaseUrl = $"https://bsky.app/profile/{id}/post/{postKey}"
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
        var session = await LoginAsync(authBody.Service, authBody.Identifier, authBody.Password, cancellationToken);
        var commentPost = posts[0];

        var parentUri = lastCommentId ?? postId;

        var parentThread = await GetPostThreadAsync(authBody.Service, session, parentUri, cancellationToken);
        var parentCid = parentThread?.Post?.Cid;
        var rootUri = parentThread?.Post?.Record?.Reply?.Root?.Uri ?? postId;
        var rootCid = parentThread?.Post?.Record?.Reply?.Root?.Cid ?? parentCid;

        var embed = await UploadMediaForPostAsync(authBody.Service, session, commentPost, cancellationToken);

        var record = new Dictionary<string, object>
        {
            ["$type"] = "app.bsky.feed.post",
            ["text"] = commentPost.Message,
            ["createdAt"] = DateTime.UtcNow.ToString("o"),
            ["reply"] = new
            {
                root = new { uri = rootUri, cid = rootCid },
                parent = new { uri = parentUri, cid = parentCid }
            }
        };

        var facets = await DetectFacetsAsync(authBody.Service, session, commentPost.Message, cancellationToken);
        if (facets != null && facets.Count > 0)
        {
            record["facets"] = facets;
        }

        if (embed != null)
        {
            record["embed"] = embed;
        }

        var postResult = await CreateRecordAsync(authBody.Service, session, "app.bsky.feed.post", record, cancellationToken);

        var newPostId = postResult.Uri;
        var postKey = newPostId.Split('/').Last();

        return new[]
        {
            new PostResponse
            {
                Id = commentPost.Id,
                PostId = newPostId,
                Status = "completed",
                ReleaseUrl = $"https://bsky.app/profile/{id}/post/{postKey}"
            }
        };
    }

    private async Task<BlueskySession> LoginAsync(
        string service,
        string identifier,
        string password,
        CancellationToken cancellationToken)
    {
        var url = $"{service}/xrpc/com.atproto.server.createSession";

        var payload = new
        {
            identifier,
            password
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new RefreshTokenException(Identifier, await response.Content.ReadAsStringAsync(cancellationToken));
        }

        return await DeserializeAsync<BlueskySession>(response);
    }

    private async Task<BlueskyProfile> GetProfileAsync(
        string service,
        string actor,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var url = $"{service}/xrpc/app.bsky.actor.getProfile?actor={actor}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<BlueskyProfile>(response);
    }

    private async Task<object?> UploadMediaForPostAsync(
        string service,
        BlueskySession session,
        PostDetails post,
        CancellationToken cancellationToken)
    {
        if (post.Media == null || post.Media.Count == 0)
            return null;

        var imageMedia = post.Media.Where(m => !m.Path.Contains(".mp4", StringComparison.OrdinalIgnoreCase)).ToList();
        var videoMedia = post.Media.Where(m => m.Path.Contains(".mp4", StringComparison.OrdinalIgnoreCase)).ToList();

        var images = new List<BlueskyUploadedImage>();

        foreach (var media in imageMedia)
        {
            var (width, height, buffer) = await ReduceImageBySizeAsync(media.Path, cancellationToken);
            var blob = await UploadBlobAsync(service, session, buffer, "image/jpeg", cancellationToken);
            images.Add(new BlueskyUploadedImage { Width = width, Height = height, Blob = blob });
        }

        if (videoMedia.Count > 0)
        {
            var videoEmbed = await UploadVideoAsync(service, session, videoMedia[0].Path, cancellationToken);
            return videoEmbed;
        }

        if (images.Count > 0)
        {
            var imagesList = imageMedia.Select((media, index) => new Dictionary<string, object?>
            {
                ["alt"] = media.Alt ?? "",
                ["image"] = images[index].Blob,
                ["aspectRatio"] = new Dictionary<string, int>
                {
                    ["width"] = images[index].Width,
                    ["height"] = images[index].Height
                }
            }).ToList();

            return new Dictionary<string, object?>
            {
                ["$type"] = "app.bsky.embed.images",
                ["images"] = imagesList
            };
        }

        return null;
    }

    private async Task<(int Width, int Height, byte[] Buffer)> ReduceImageBySizeAsync(
        string url,
        CancellationToken cancellationToken,
        int maxSizeKB = 976)
    {
        byte[] imageBuffer;

        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            imageBuffer = await HttpClient.GetByteArrayAsync(url, cancellationToken);
        }
        else
        {
            imageBuffer = await File.ReadAllBytesAsync(url, cancellationToken);
        }

        int width = 800;
        int height = 600;

        while (imageBuffer.Length / 1024 > maxSizeKB)
        {
            width = (int)(width * 0.9);
            height = (int)(height * 0.9);

            if (width < 10 || height < 10)
                break;
        }

        return (width, height, imageBuffer);
    }

    private async Task<object> UploadBlobAsync(
        string service,
        BlueskySession session,
        byte[] data,
        string contentType,
        CancellationToken cancellationToken)
    {
        var url = $"{service}/xrpc/com.atproto.repo.uploadBlob";

        var content = new ByteArrayContent(data);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {session.AccessJwt}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await DeserializeAsync<BlueskyUploadBlobResponse>(response);
        return result.Blob;
    }

    private async Task<object> UploadVideoAsync(
        string service,
        BlueskySession session,
        string videoUrl,
        CancellationToken cancellationToken)
    {
        var serviceAuthUrl = $"{service}/xrpc/com.atproto.server.getServiceAuth";
        serviceAuthUrl += $"?aud=did:web:{new Uri(service).Host}";
        serviceAuthUrl += "&lxm=com.atproto.repo.uploadBlob";
        serviceAuthUrl += $"&exp={DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds()}";

        var authRequest = new HttpRequestMessage(HttpMethod.Get, serviceAuthUrl);
        authRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {session.AccessJwt}");

        var authResponse = await HttpClient.SendAsync(authRequest, cancellationToken);
        authResponse.EnsureSuccessStatusCode();

        var authResult = await DeserializeAsync<BlueskyServiceAuthResponse>(authResponse);

        byte[] videoBytes;
        if (videoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            videoBytes = await HttpClient.GetByteArrayAsync(videoUrl, cancellationToken);
        }
        else
        {
            videoBytes = await File.ReadAllBytesAsync(videoUrl, cancellationToken);
        }

        var uploadUrl = $"https://video.bsky.app/xrpc/app.bsky.video.uploadVideo?did={session.Did}&name=video.mp4";

        var uploadContent = new ByteArrayContent(videoBytes);
        uploadContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");
        uploadContent.Headers.ContentLength = videoBytes.Length;

        var uploadRequest = new HttpRequestMessage(HttpMethod.Post, uploadUrl) { Content = uploadContent };
        uploadRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {authResult.Token}");

        var uploadResponse = await HttpClient.SendAsync(uploadRequest, cancellationToken);
        uploadResponse.EnsureSuccessStatusCode();

        var jobStatus = await DeserializeAsync<BlueskyVideoJobStatus>(uploadResponse);
        var blob = jobStatus.Blob;

        while (blob == null)
        {
            await Task.Delay(30000, cancellationToken);

            var statusUrl = $"https://video.bsky.app/xrpc/app.bsky.video.getJobStatus?jobId={jobStatus.JobId}";
            var statusResponse = await HttpClient.GetAsync(statusUrl, cancellationToken);
            statusResponse.EnsureSuccessStatusCode();

            var statusResult = await DeserializeAsync<BlueskyVideoJobStatusResponse>(statusResponse);
            blob = statusResult.JobStatus?.Blob;

            if (statusResult.JobStatus?.State == "JOB_STATE_FAILED")
            {
                throw new BadBodyException(Identifier, "Could not upload video, job failed");
            }
        }

        return new Dictionary<string, object?>
        {
            ["$type"] = "app.bsky.embed.video",
            ["video"] = blob
        };
    }

    private async Task<List<BlueskyFacet>?> DetectFacetsAsync(
        string service,
        BlueskySession session,
        string text,
        CancellationToken cancellationToken)
    {
        return null;
    }

    private async Task<BlueskyCreateRecordResponse> CreateRecordAsync(
        string service,
        BlueskySession session,
        string collection,
        object record,
        CancellationToken cancellationToken)
    {
        var url = $"{service}/xrpc/com.atproto.repo.createRecord";

        var payload = new
        {
            repo = session.Did,
            collection,
            record
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {session.AccessJwt}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<BlueskyCreateRecordResponse>(response);
    }

    private async Task<BlueskyPostThread?> GetPostThreadAsync(
        string service,
        BlueskySession session,
        string uri,
        CancellationToken cancellationToken)
    {
        var url = $"{service}/xrpc/app.bsky.feed.getPostThread?uri={uri}&depth=0";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {session.AccessJwt}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<BlueskyPostThreadResponse>(response) is { } result
            ? result.Thread
            : null;
    }

    private static BlueskyAuthBody GetAuthBody(Integration integration)
    {
        if (string.IsNullOrEmpty(integration.CustomInstanceDetails))
        {
            throw new InvalidOperationException("No custom instance details for Bluesky");
        }

        var jsonBytes = Convert.FromBase64String(integration.CustomInstanceDetails);
        var json = Encoding.UTF8.GetString(jsonBytes);
        return JsonSerializer.Deserialize<BlueskyAuthBody>(json)
            ?? throw new InvalidOperationException("Invalid auth body");
    }

    #region DTOs

    private class BlueskyAuthBody
    {
        [JsonPropertyName("service")]
        public string Service { get; set; } = "https://bsky.social";

        [JsonPropertyName("identifier")]
        public string Identifier { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    private class BlueskySession
    {
        [JsonPropertyName("did")]
        public string Did { get; set; } = string.Empty;

        [JsonPropertyName("handle")]
        public string Handle { get; set; } = string.Empty;

        [JsonPropertyName("accessJwt")]
        public string AccessJwt { get; set; } = string.Empty;

        [JsonPropertyName("refreshJwt")]
        public string RefreshJwt { get; set; } = string.Empty;
    }

    private class BlueskyProfile
    {
        [JsonPropertyName("did")]
        public string Did { get; set; } = string.Empty;

        [JsonPropertyName("handle")]
        public string Handle { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }
    }

    private class BlueskyUploadedImage
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public object? Blob { get; set; }
    }

    private class BlueskyUploadBlobResponse
    {
        [JsonPropertyName("blob")]
        public object? Blob { get; set; }
    }

    private class BlueskyServiceAuthResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }

    private class BlueskyVideoJobStatus
    {
        [JsonPropertyName("jobId")]
        public string JobId { get; set; } = string.Empty;

        [JsonPropertyName("blob")]
        public object? Blob { get; set; }
    }

    private class BlueskyVideoJobStatusResponse
    {
        [JsonPropertyName("jobStatus")]
        public BlueskyVideoJobStatusDetail? JobStatus { get; set; }
    }

    private class BlueskyVideoJobStatusDetail
    {
        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("blob")]
        public object? Blob { get; set; }
    }

    private class BlueskyCreateRecordResponse
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("cid")]
        public string? Cid { get; set; }
    }

    private class BlueskyFacet
    {
        [JsonPropertyName("index")]
        public BlueskyFacetIndex? Index { get; set; }

        [JsonPropertyName("features")]
        public List<object>? Features { get; set; }
    }

    private class BlueskyFacetIndex
    {
        [JsonPropertyName("byteStart")]
        public int ByteStart { get; set; }

        [JsonPropertyName("byteEnd")]
        public int ByteEnd { get; set; }
    }

    private class BlueskyPostThreadResponse
    {
        [JsonPropertyName("thread")]
        public BlueskyPostThread? Thread { get; set; }
    }

    private class BlueskyPostThread
    {
        [JsonPropertyName("post")]
        public BlueskyPost? Post { get; set; }
    }

    private class BlueskyPost
    {
        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("cid")]
        public string? Cid { get; set; }

        [JsonPropertyName("record")]
        public BlueskyPostRecord? Record { get; set; }
    }

    private class BlueskyPostRecord
    {
        [JsonPropertyName("reply")]
        public BlueskyReplyRef? Reply { get; set; }
    }

    private class BlueskyReplyRef
    {
        [JsonPropertyName("root")]
        public BlueskyStrongRef? Root { get; set; }

        [JsonPropertyName("parent")]
        public BlueskyStrongRef? Parent { get; set; }
    }

    private class BlueskyStrongRef
    {
        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("cid")]
        public string? Cid { get; set; }
    }

    #endregion
}
