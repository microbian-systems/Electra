using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class DiscordProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "discord";
    public override string Name => "Discord";
    public override string[] Scopes => new[] { "identify", "guilds" };
    public override EditorType Editor => EditorType.Markdown;
    public override int MaxConcurrentJobs => 5;

    public DiscordProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<DiscordProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 1980;

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(6);
        var clientId = GetClientId();
        var frontendUrl = GetFrontendUrl();

        var url = $"https://discord.com/oauth2/authorize" +
                  $"?client_id={clientId}" +
                  $"&permissions=377957124096" +
                  $"&response_type=code" +
                  $"&redirect_uri={Uri.EscapeDataString($"{frontendUrl}/integrations/social/discord")}" +
                  $"&integration_type=0" +
                  $"&scope=bot+identify+guilds" +
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
        var tokenResponse = await ExchangeCodeForTokenAsync(parameters.Code, cancellationToken);
        
        CheckScopes(Scopes, tokenResponse.Scope.Split(' '));

        var applicationInfo = await GetApplicationInfoAsync(tokenResponse.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            Id = tokenResponse.Guild?.Id ?? string.Empty,
            Name = applicationInfo.Name,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresIn = tokenResponse.ExpiresIn,
            Picture = $"https://cdn.discordapp.com/avatars/{applicationInfo.Bot.Id}/{applicationInfo.Bot.Avatar}.png",
            Username = applicationInfo.Bot.Username
        };
    }

    public override async Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var credentials = GetBasicCredentials();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token")
        {
            Content = content
        };
        request.Headers.Add("Authorization", $"Basic {credentials}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await DeserializeAsync<DiscordTokenResponse>(response);

        var applicationInfo = await GetApplicationInfoAsync(tokenResponse.AccessToken, cancellationToken);

        return new AuthTokenDetails
        {
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresIn = tokenResponse.ExpiresIn,
            AccessToken = tokenResponse.AccessToken,
            Id = string.Empty,
            Name = applicationInfo.Name,
            Picture = string.Empty,
            Username = string.Empty
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
        var channel = firstPost.Settings?.GetValueOrDefault("channel")?.ToString() 
            ?? throw new ArgumentException("Channel is required");

        var form = new MultipartFormDataContent();
        
        var message = FormatMessage(firstPost.Message);
        var payload = new
        {
            content = message,
            attachments = firstPost.Media?.Select((m, index) => new
            {
                id = index,
                description = $"Picture {index}",
                filename = GetFileName(m.Path)
            }).ToList()
        };

        form.Add(new StringContent(JsonSerializer.Serialize(payload)), "payload_json");

        if (firstPost.Media != null)
        {
            for (var i = 0; i < firstPost.Media.Count; i++)
            {
                var media = firstPost.Media[i];
                var bytes = await ReadOrFetchAsync(media.Path, cancellationToken);
                form.Add(new ByteArrayContent(bytes), $"files[{i}]", GetFileName(media.Path));
            }
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"https://discord.com/api/channels/{channel}/messages")
        {
            Content = form
        };
        request.Headers.Add("Authorization", $"Bot {GetBotToken()}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var messageResponse = await DeserializeAsync<DiscordMessageResponse>(response);

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = messageResponse.Id,
                ReleaseUrl = $"https://discord.com/channels/{id}/{channel}/{messageResponse.Id}",
                Status = "success"
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
        var channel = commentPost.Settings?.GetValueOrDefault("channel")?.ToString()
            ?? throw new ArgumentException("Channel is required");

        string threadChannel;

        if (string.IsNullOrEmpty(lastCommentId))
        {
            var threadResponse = await CreateThreadAsync(channel, postId, cancellationToken);
            threadChannel = threadResponse.Id;
        }
        else
        {
            threadChannel = channel;
        }

        var form = new MultipartFormDataContent();
        
        var message = FormatMessage(commentPost.Message);
        var payload = new
        {
            content = message,
            attachments = commentPost.Media?.Select((m, index) => new
            {
                id = index,
                description = $"Picture {index}",
                filename = GetFileName(m.Path)
            }).ToList()
        };

        form.Add(new StringContent(JsonSerializer.Serialize(payload)), "payload_json");

        if (commentPost.Media != null)
        {
            for (var i = 0; i < commentPost.Media.Count; i++)
            {
                var media = commentPost.Media[i];
                var bytes = await ReadOrFetchAsync(media.Path, cancellationToken);
                form.Add(new ByteArrayContent(bytes), $"files[{i}]", GetFileName(media.Path));
            }
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"https://discord.com/api/channels/{threadChannel}/messages")
        {
            Content = form
        };
        request.Headers.Add("Authorization", $"Bot {GetBotToken()}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var messageResponse = await DeserializeAsync<DiscordMessageResponse>(response);

        return new[]
        {
            new PostResponse
            {
                Id = commentPost.Id,
                PostId = messageResponse.Id,
                ReleaseUrl = $"https://discord.com/channels/{id}/{threadChannel}/{messageResponse.Id}",
                Status = "success"
            }
        };
    }

    public override async Task<object?> MentionAsync(
        string token,
        MentionQuery query,
        string id,
        Integration integration,
        CancellationToken cancellationToken = default)
    {
        var rolesTask = GetRolesAsync(id, cancellationToken);
        var membersTask = SearchMembersAsync(id, query.Query, cancellationToken);

        await Task.WhenAll(rolesTask, membersTask);

        var roles = await rolesTask;
        var members = await membersTask;

        var result = new List<MentionResult>();

        var specialMentions = new[]
        {
            new MentionResult { Id = "here", Label = "here", Image = string.Empty, DoNotCache = true },
            new MentionResult { Id = "everyone", Label = "everyone", Image = string.Empty, DoNotCache = true }
        }.Where(m => m.Label.Contains(query.Query, StringComparison.OrdinalIgnoreCase));

        result.AddRange(specialMentions);
        result.AddRange(roles
            .Where(r => r.Name.Contains(query.Query, StringComparison.OrdinalIgnoreCase) &&
                        r.Name != "@everyone" && r.Name != "@here")
            .Select(r => new MentionResult
            {
                Id = $"&{r.Id}",
                Label = r.Name.TrimStart('@'),
                Image = string.Empty,
                DoNotCache = true
            }));
        result.AddRange(members.Select(m => new MentionResult
        {
            Id = m.User.Id,
            Label = m.User.GlobalName ?? m.User.Username,
            Image = $"https://cdn.discordapp.com/avatars/{m.User.Id}/{m.User.Avatar}.png"
        }));

        return result;
    }

    public override string? MentionFormat(string idOrHandle, string name)
    {
        if (name == "@here" || name == "@everyone")
        {
            return name;
        }

        return $"[[[@{idOrHandle.TrimStart('@')}]]]";
    }

    private async Task<DiscordTokenResponse> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken)
    {
        var credentials = GetBasicCredentials();
        var frontendUrl = GetFrontendUrl();
        
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = $"{frontendUrl}/integrations/social/discord"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token")
        {
            Content = content
        };
        request.Headers.Add("Authorization", $"Basic {credentials}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<DiscordTokenResponse>(response);
    }

    private async Task<DiscordApplicationInfo> GetApplicationInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/oauth2/@me");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<DiscordApplicationInfo>(response);
    }

    private async Task<DiscordThreadResponse> CreateThreadAsync(string channelId, string messageId, CancellationToken cancellationToken)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { name = "Thread", auto_archive_duration = 1440 }),
            System.Text.Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"https://discord.com/api/channels/{channelId}/messages/{messageId}/threads")
        {
            Content = content
        };
        request.Headers.Add("Authorization", $"Bot {GetBotToken()}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<DiscordThreadResponse>(response);
    }

    private async Task<List<DiscordRole>> GetRolesAsync(string guildId, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/guilds/{guildId}/roles");
        request.Headers.Add("Authorization", $"Bot {GetBotToken()}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<List<DiscordRole>>(response);
    }

    private async Task<List<DiscordMember>> SearchMembersAsync(string guildId, string query, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/guilds/{guildId}/members/search?query={Uri.EscapeDataString(query)}");
        request.Headers.Add("Authorization", $"Bot {GetBotToken()}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await DeserializeAsync<List<DiscordMember>>(response);
    }

    private static string FormatMessage(string message)
    {
        return System.Text.RegularExpressions.Regex.Replace(message, @"\[\[\[(@.*?)]]]", match =>
        {
            return $"<{match.Groups[1].Value}>";
        });
    }

    private static string GetFileName(string path)
    {
        return path.Split('/').Last();
    }

    private string GetClientId() => _configuration["DISCORD_CLIENT_ID"] ?? throw new InvalidOperationException("DISCORD_CLIENT_ID not configured");
    private string GetClientSecret() => _configuration["DISCORD_CLIENT_SECRET"] ?? throw new InvalidOperationException("DISCORD_CLIENT_SECRET not configured");
    private string GetBotToken() => _configuration["DISCORD_BOT_TOKEN_ID"] ?? throw new InvalidOperationException("DISCORD_BOT_TOKEN_ID not configured");
    private string GetFrontendUrl() => _configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL not configured");
    
    private string GetBasicCredentials()
    {
        var credentials = $"{GetClientId()}:{GetClientSecret()}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
    }

    #region DTOs

    private class DiscordTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;
        
        [JsonPropertyName("guild")]
        public DiscordGuild? Guild { get; set; }
    }

    private class DiscordGuild
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class DiscordApplicationInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("bot")]
        public DiscordBot Bot { get; set; } = new();
    }

    private class DiscordBot
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
        
        [JsonPropertyName("avatar")]
        public string Avatar { get; set; } = string.Empty;
    }

    private class DiscordMessageResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class DiscordThreadResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class DiscordRole
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private class DiscordMember
    {
        [JsonPropertyName("user")]
        public DiscordUser User { get; set; } = new();
    }

    private class DiscordUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
        
        [JsonPropertyName("global_name")]
        public string? GlobalName { get; set; }
        
        [JsonPropertyName("avatar")]
        public string Avatar { get; set; } = string.Empty;
    }

    #endregion
}
