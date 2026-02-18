using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class VkProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "vk";
    public override string Name => "VK";
    public override string[] Scopes => new[]
    {
        "vkid.personal_info",
        "email",
        "wall",
        "status",
        "docs",
        "photos",
        "video"
    };

    public override int MaxConcurrentJobs => 2;
    public override int MaxLength(object? additionalSettings = null) => 2048;

    public VkProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<VkProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(32);
        var codeVerifier = GenerateCodeVerifier();
        var challenge = GenerateCodeChallenge(codeVerifier);

        var clientId = GetClientId();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = frontendUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase)
            ? $"{frontendUrl}/integrations/social/vk"
            : $"https://redirectmeto.com/{frontendUrl}/integrations/social/vk";

        var url = $"https://id.vk.com/authorize" +
                  $"?response_type=code" +
                  $"&client_id={clientId}" +
                  $"&code_challenge_method=S256" +
                  $"&code_challenge={challenge}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
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
        var codeParts = parameters.Code.Split("&&&&");
        var code = codeParts[0];
        var deviceId = codeParts.Length > 1 ? codeParts[1] : MakeId(32);

        var clientId = GetClientId();
        var frontendUrl = GetFrontendUrl();
        var redirectUri = frontendUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase)
            ? $"{frontendUrl}/integrations/social/vk"
            : $"https://redirectmeto.com/{frontendUrl}/integrations/social/vk";

        var formData = new MultipartFormDataContent
        {
            { new StringContent(clientId), "client_id" },
            { new StringContent("authorization_code"), "grant_type" },
            { new StringContent(parameters.CodeVerifier ?? string.Empty), "code_verifier" },
            { new StringContent(deviceId), "device_id" },
            { new StringContent(code), "code" },
            { new StringContent(redirectUri), "redirect_uri" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://id.vk.com/oauth2/auth")
        {
            Content = formData
        };

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await DeserializeAsync<VkTokenResponse>(response);

        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.UserId,
            Name = $"{userInfo.FirstName} {userInfo.LastName}",
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = $"{tokenResponse.RefreshToken}&&&&{deviceId}",
            ExpiresIn = tokenResponse.ExpiresIn,
            Picture = userInfo.Avatar ?? string.Empty,
            Username = userInfo.FirstName.ToLowerInvariant()
        };
    }

    public override async Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var parts = refreshToken.Split("&&&&");
        var oldRefreshToken = parts[0];
        var deviceId = parts.Length > 1 ? parts[1] : MakeId(32);

        var clientId = GetClientId();

        var formData = new MultipartFormDataContent
        {
            { new StringContent("refresh_token"), "grant_type" },
            { new StringContent(oldRefreshToken), "refresh_token" },
            { new StringContent(clientId), "client_id" },
            { new StringContent(deviceId), "device_id" },
            { new StringContent(MakeId(32)), "state" },
            { new StringContent(string.Join(" ", Scopes)), "scope" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://id.vk.com/oauth2/auth")
        {
            Content = formData
        };

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await DeserializeAsync<VkTokenResponse>(response);
        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = userInfo.UserId,
            Name = $"{userInfo.FirstName} {userInfo.LastName}",
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = $"{tokenResponse.RefreshToken}&&&&{deviceId}",
            ExpiresIn = tokenResponse.ExpiresIn,
            Picture = userInfo.Avatar ?? string.Empty,
            Username = userInfo.FirstName.ToLowerInvariant()
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

        var mediaList = await UploadMediaAsync(id, accessToken, firstPost, cancellationToken);

        var formData = new MultipartFormDataContent
        {
            { new StringContent(firstPost.Message), "message" }
        };

        if (mediaList.Count > 0)
        {
            var attachments = string.Join(",", mediaList.Select(m => $"{m.Type}{id}_{m.Id}"));
            formData.Add(new StringContent(attachments), "attachments");
        }

        var clientId = GetClientId();
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.vk.com/method/wall.post?v=5.251&access_token={accessToken}&client_id={clientId}")
        {
            Content = formData
        };

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var postResponse = await DeserializeAsync<VkWallPostResponse>(response);

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = postResponse.Response?.PostId.ToString() ?? string.Empty,
                ReleaseUrl = $"https://vk.com/feed?w=wall{id}_{postResponse.Response?.PostId}",
                Status = "completed"
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

        var mediaList = await UploadMediaAsync(id, accessToken, commentPost, cancellationToken);

        var formData = new MultipartFormDataContent
        {
            { new StringContent(commentPost.Message), "message" },
            { new StringContent(postId), "post_id" }
        };

        if (mediaList.Count > 0)
        {
            var attachments = string.Join(",", mediaList.Select(m => $"{m.Type}{id}_{m.Id}"));
            formData.Add(new StringContent(attachments), "attachments");
        }

        var clientId = GetClientId();
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.vk.com/method/wall.createComment?v=5.251&access_token={accessToken}&client_id={clientId}")
        {
            Content = formData
        };

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var commentResponse = await DeserializeAsync<VkCommentResponse>(response);

        return new[]
        {
            new PostResponse
            {
                Id = commentPost.Id,
                PostId = commentResponse.Response?.CommentId.ToString() ?? string.Empty,
                ReleaseUrl = $"https://vk.com/feed?w=wall{id}_{postId}",
                Status = "completed"
            }
        };
    }

    private async Task<VkUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var clientId = GetClientId();

        var formData = new MultipartFormDataContent
        {
            { new StringContent(clientId), "client_id" },
            { new StringContent(accessToken), "access_token" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://id.vk.com/oauth2/user_info")
        {
            Content = formData
        };

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var userInfoResponse = await DeserializeAsync<VkUserInfoResponse>(response);
        return userInfoResponse.User;
    }

    private async Task<List<VkMedia>> UploadMediaAsync(string userId, string accessToken, PostDetails post, CancellationToken cancellationToken)
    {
        var mediaList = new List<VkMedia>();

        if (post.Media == null || post.Media.Count == 0)
        {
            return mediaList;
        }

        foreach (var media in post.Media)
        {
            var isVideo = media.Path.Contains(".mp4", StringComparison.OrdinalIgnoreCase);

            if (isVideo)
            {
                var uploadServerRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api.vk.com/method/video.save?access_token={accessToken}&v=5.251");
                var uploadServerResponse = await HttpClient.SendAsync(uploadServerRequest, cancellationToken);
                var uploadServerData = await DeserializeAsync<VkVideoUploadServerResponse>(uploadServerResponse);

                var mediaBytes = await ReadOrFetchAsync(media.Path, cancellationToken);

                var fileName = media.Path.Split('/').Last();
                var uploadContent = new MultipartFormDataContent
                {
                    { new ByteArrayContent(mediaBytes), "video_file", fileName }
                };

                await HttpClient.PostAsync(uploadServerData.Response.UploadUrl, uploadContent, cancellationToken);

                mediaList.Add(new VkMedia
                {
                    Id = uploadServerData.Response.VideoId,
                    Type = "video"
                });
            }
            else
            {
                var uploadServerRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api.vk.com/method/photos.getWallUploadServer?owner_id={userId}&access_token={accessToken}&v=5.251");
                var uploadServerResponse = await HttpClient.SendAsync(uploadServerRequest, cancellationToken);
                var uploadServerData = await DeserializeAsync<VkPhotoUploadServerResponse>(uploadServerResponse);

                var mediaBytes = await ReadOrFetchAsync(media.Path, cancellationToken);

                var fileName = media.Path.Split('/').Last();
                var uploadContent = new MultipartFormDataContent
                {
                    { new ByteArrayContent(mediaBytes), "photo", fileName }
                };

                var uploadResponse = await HttpClient.PostAsync(uploadServerData.Response.UploadUrl, uploadContent, cancellationToken);
                var uploadResult = await DeserializeAsync<VkPhotoUploadResult>(uploadResponse);

                var saveFormData = new MultipartFormDataContent
                {
                    { new StringContent(uploadResult.Photo), "photo" },
                    { new StringContent(uploadResult.Server.ToString()), "server" },
                    { new StringContent(uploadResult.Hash), "hash" }
                };

                var saveRequest = new HttpRequestMessage(HttpMethod.Post, $"https://api.vk.com/method/photos.saveWallPhoto?access_token={accessToken}&v=5.251")
                {
                    Content = saveFormData
                };

                var saveResponse = await HttpClient.SendAsync(saveRequest, cancellationToken);
                var saveResult = await DeserializeAsync<VkSavePhotoResponse>(saveResponse);

                if (saveResult.Response != null && saveResult.Response.Count > 0)
                {
                    mediaList.Add(new VkMedia
                    {
                        Id = saveResult.Response[0].Id.ToString(),
                        Type = "photo"
                    });
                }
            }
        }

        return mediaList;
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var bytes = Encoding.UTF8.GetBytes(codeVerifier);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private string GetClientId() => _configuration["VK_ID"] ?? throw new InvalidOperationException("VK_ID not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");

    #region DTOs

    private class VkMedia
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    private class VkTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }

    private class VkUserInfoResponse
    {
        [JsonPropertyName("user")]
        public VkUserInfo User { get; set; } = new();
    }

    private class VkUserInfo
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("last_name")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }
    }

    private class VkWallPostResponse
    {
        [JsonPropertyName("response")]
        public VkWallPostResponseData? Response { get; set; }
    }

    private class VkWallPostResponseData
    {
        [JsonPropertyName("post_id")]
        public int PostId { get; set; }
    }

    private class VkCommentResponse
    {
        [JsonPropertyName("response")]
        public VkCommentResponseData? Response { get; set; }
    }

    private class VkCommentResponseData
    {
        [JsonPropertyName("comment_id")]
        public int CommentId { get; set; }
    }

    private class VkPhotoUploadServerResponse
    {
        [JsonPropertyName("response")]
        public VkUploadServerData Response { get; set; } = new();
    }

    private class VkVideoUploadServerResponse
    {
        [JsonPropertyName("response")]
        public VkVideoUploadData Response { get; set; } = new();
    }

    private class VkUploadServerData
    {
        [JsonPropertyName("upload_url")]
        public string UploadUrl { get; set; } = string.Empty;
    }

    private class VkVideoUploadData
    {
        [JsonPropertyName("upload_url")]
        public string UploadUrl { get; set; } = string.Empty;

        [JsonPropertyName("video_id")]
        public string VideoId { get; set; } = string.Empty;
    }

    private class VkPhotoUploadResult
    {
        [JsonPropertyName("photo")]
        public string Photo { get; set; } = string.Empty;

        [JsonPropertyName("server")]
        public int Server { get; set; }

        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;
    }

    private class VkSavePhotoResponse
    {
        [JsonPropertyName("response")]
        public List<VkSavedPhoto>? Response { get; set; }
    }

    private class VkSavedPhoto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    #endregion
}
