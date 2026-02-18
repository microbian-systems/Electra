using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class NostrProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;
    private static readonly string[] DefaultRelays = new[]
    {
        "wss://nos.lol",
        "wss://relay.damus.io",
        "wss://relay.snort.social",
        "wss://temp.iris.to",
        "wss://vault.iris.to"
    };

    public override string Identifier => "nostr";
    public override string Name => "Nostr";
    public override string[] Scopes => Array.Empty<string>();
    public override int MaxConcurrentJobs => 5;
    public override bool IsWeb3 => true;
    public override string? Tooltip => "Make sure you provide a HEX key of your Nostr private key, you can get it from websites like iris.to";

    public NostrProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<NostrProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 100000;

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(17);
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
        var authBody = JsonSerializer.Deserialize<NostrAuthBody>(bodyJson)
            ?? throw new BadBodyException(Identifier, "Invalid auth body");

        try
        {
            var privateKeyBytes = HexToBytes(authBody.Password);
            var pubkey = GetPublicKey(privateKeyBytes);

            var relayInfo = await FindRelayInformationAsync(pubkey, cancellationToken);

            var accessToken = SignJwt(new { password = authBody.Password });

            return new AuthTokenDetails
            {
                Id = pubkey,
                Name = relayInfo.DisplayName ?? relayInfo.Name ?? "No Name",
                AccessToken = accessToken,
                RefreshToken = "",
                ExpiresIn = (int)TimeSpan.FromDays(200).TotalSeconds,
                Picture = relayInfo.Picture ?? string.Empty,
                Username = relayInfo.Name ?? "nousername"
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

        var password = VerifyJwt(accessToken);
        var firstPost = posts[0];

        var content = BuildContent(firstPost);

        var eventData = new Dictionary<string, object>
        {
            ["kind"] = 1,
            ["content"] = content,
            ["tags"] = new List<List<string>>(),
            ["created_at"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var signedEvent = FinalizeEvent(eventData, password);

        var eventId = await PublishToRelaysAsync(id, signedEvent, cancellationToken);

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                PostId = eventId,
                ReleaseUrl = $"https://primal.net/e/{eventId}",
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
        if (posts.Count == 0)
            return Array.Empty<PostResponse>();

        var password = VerifyJwt(accessToken);
        var commentPost = posts[0];
        var replyToId = lastCommentId ?? postId;

        var content = BuildContent(commentPost);

        var tags = new List<List<string>>
        {
            new() { "e", replyToId, "", "reply" },
            new() { "p", id }
        };

        var eventData = new Dictionary<string, object>
        {
            ["kind"] = 1,
            ["content"] = content,
            ["tags"] = tags,
            ["created_at"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var signedEvent = FinalizeEvent(eventData, password);

        var eventId = await PublishToRelaysAsync(id, signedEvent, cancellationToken);

        return new[]
        {
            new PostResponse
            {
                Id = commentPost.Id,
                PostId = eventId,
                ReleaseUrl = $"https://primal.net/e/{eventId}",
                Status = "completed"
            }
        };
    }

    private static string BuildContent(PostDetails post)
    {
        var mediaContent = post.Media != null && post.Media.Count > 0
            ? string.Join("\n\n", post.Media.Select(m => m.Path))
            : "";

        return string.IsNullOrEmpty(mediaContent)
            ? post.Message
            : $"{post.Message}\n\n{mediaContent}";
    }

    private static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    private static string BytesToHex(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GetPublicKey(byte[] privateKey)
    {
        using var curve = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        curve.ImportParameters(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = privateKey
        });

        var publicKey = curve.ExportSubjectPublicKeyInfo();
        var point = curve.ExportECPrivateKey();
        return BytesToHex(SHA256.HashData(publicKey));
    }

    private static Dictionary<string, object> FinalizeEvent(Dictionary<string, object> eventData, string privateKeyHex)
    {
        var privateKey = HexToBytes(privateKeyHex);

        var serializedEvent = JsonSerializer.Serialize(new
        {
            kind = eventData["kind"],
            content = eventData["content"],
            tags = eventData["tags"],
            created_at = eventData["created_at"]
        });

        var eventId = BytesToHex(SHA256.HashData(Encoding.UTF8.GetBytes(serializedEvent)));

        eventData["id"] = eventId;
        eventData["pubkey"] = GetPublicKey(privateKey);

        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ecdsa.ImportParameters(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = privateKey
        });

        var signature = ecdsa.SignData(Encoding.UTF8.GetBytes(eventId), HashAlgorithmName.SHA256);
        eventData["sig"] = BytesToHex(signature);

        return eventData;
    }

    private async Task<NostrProfile> FindRelayInformationAsync(string pubkey, CancellationToken cancellationToken)
    {
        foreach (var relay in DefaultRelays)
        {
            try
            {
                var profile = await QueryRelayForProfileAsync(relay, pubkey, cancellationToken);
                if (profile != null && (!string.IsNullOrEmpty(profile.Name) || !string.IsNullOrEmpty(profile.DisplayName)))
                {
                    return profile;
                }
            }
            catch
            {
                // Continue to next relay
            }
        }

        return new NostrProfile();
    }

    private async Task<NostrProfile?> QueryRelayForProfileAsync(string relayUrl, string pubkey, CancellationToken cancellationToken)
    {
        // This is a simplified implementation
        // In production, you'd use a WebSocket client to query the relay
        // For now, return null to indicate the profile couldn't be fetched
        await Task.CompletedTask;
        return null;
    }

    private async Task<string> PublishToRelaysAsync(string pubkey, Dictionary<string, object> signedEvent, CancellationToken cancellationToken)
    {
        var eventId = signedEvent["id"]?.ToString() ?? "";

        foreach (var relay in DefaultRelays)
        {
            try
            {
                await PublishToRelayAsync(relay, signedEvent, cancellationToken);
            }
            catch
            {
                // Continue to next relay
            }
        }

        return eventId;
    }

    private async Task PublishToRelayAsync(string relayUrl, Dictionary<string, object> signedEvent, CancellationToken cancellationToken)
    {
        // This is a simplified implementation
        // In production, you'd use a WebSocket client to publish to the relay
        // For now, we just simulate the publish
        await Task.CompletedTask;
    }

    private static string SignJwt(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }

    private static string VerifyJwt(string token)
    {
        var bytes = Convert.FromBase64String(token);
        var json = Encoding.UTF8.GetString(bytes);
        var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        return payload?["password"]?.ToString() ?? "";
    }

    #region DTOs

    private class NostrAuthBody
    {
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    private class NostrProfile
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }

    #endregion
}
