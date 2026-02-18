using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class ListmonkProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "listmonk";
    public override string Name => "ListMonk";
    public override string[] Scopes => Array.Empty<string>();
    public override EditorType Editor => EditorType.Html;
    public override int MaxConcurrentJobs => 100;

    public ListmonkProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ListmonkProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 100000000;

    public async Task<List<ListmonkCustomField>> GetCustomFieldsAsync()
    {
        return new List<ListmonkCustomField>
        {
            new() { Key = "url", Label = "URL", DefaultValue = "", Validation = @"^(https?:\/\/)(?:\S+(?::\S*)?@)?(?:(?:localhost)|(?:\d{1,3}(?:\.\d{1,3}){3})|(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z]{2,63})(?::\d{2,5})?(?:\/[^\s?#]*)?(?:\?[^\s#]*)?(?:#[^\s]*)?$", Type = "text" },
            new() { Key = "username", Label = "Username", Validation = @".+", Type = "text" },
            new() { Key = "password", Label = "Password", Validation = @".{3,}", Type = "password" }
        };
    }

    public override Task<AuthTokenDetails> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
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

    public override Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(6);
        return Task.FromResult(new GenerateAuthUrlResponse
        {
            Url = string.Empty,
            CodeVerifier = MakeId(10),
            State = state
        });
    }

    public override async Task<AuthTokenDetails> AuthenticateAsync(
        AuthenticateParams parameters,
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bodyBytes = Convert.FromBase64String(parameters.Code);
            var bodyJson = Encoding.UTF8.GetString(bodyBytes);
            var body = JsonSerializer.Deserialize<ListmonkAuthBody>(bodyJson);

            if (body == null)
            {
                return new AuthTokenDetails { Id = "invalid" };
            }

            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{body.Username}:{body.Password}"));

            var request = new HttpRequestMessage(HttpMethod.Get, $"{body.Url}/api/settings");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Authorization", $"Basic {basic}");

            var response = await HttpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new AuthTokenDetails { Id = "Invalid credentials" };
            }

            var settingsResponse = await DeserializeAsync<ListmonkSettingsResponse>(response);

            return new AuthTokenDetails
            {
                RefreshToken = basic,
                ExpiresIn = (int)TimeSpan.FromDays(100 * 365).TotalSeconds,
                AccessToken = basic,
                Id = Convert.ToBase64String(Encoding.UTF8.GetBytes(body.Url)),
                Name = settingsResponse.Data?.AppSiteName ?? "ListMonk",
                Picture = settingsResponse.Data?.AppLogoUrl ?? string.Empty,
                Username = settingsResponse.Data?.AppSiteName ?? "ListMonk"
            };
        }
        catch
        {
            return new AuthTokenDetails { Id = "Invalid credentials" };
        }
    }

    public async Task<List<ListmonkList>> GetListsAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        var body = GetAuthBody(integration);
        var auth = GetAuthHeader(body);

        var request = new HttpRequestMessage(HttpMethod.Get, $"{body.Url}/api/lists");
        request.Headers.Add("Authorization", $"Basic {auth}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var listsResponse = await DeserializeAsync<ListmonkListsResponse>(response);

        return (listsResponse.Data?.Results ?? [])
            .Select(p => new ListmonkList { Id = p.Id, Name = p.Name })
            .ToList();
    }

    public async Task<List<ListmonkTemplate>> GetTemplatesAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        var body = GetAuthBody(integration);
        var auth = GetAuthHeader(body);

        var request = new HttpRequestMessage(HttpMethod.Get, $"{body.Url}/api/templates");
        request.Headers.Add("Authorization", $"Basic {auth}");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var templatesResponse = await DeserializeAsync<ListmonkTemplatesResponse>(response);

        var templates = new List<ListmonkTemplate>
        {
            new() { Id = 0, Name = "Default" }
        };

        templates.AddRange((templatesResponse.Data ?? [])
            .Select(p => new ListmonkTemplate { Id = p.Id, Name = p.Name }));

        return templates;
    }

    public override async Task<PostResponse[]> PostAsync(
        string id,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken = default)
    {
        var body = GetAuthBody(integration);
        var auth = GetAuthHeader(body);

        var firstPost = posts.First();
        var settings = firstPost.Settings ?? new Dictionary<string, object>();

        var preview = GetSettingValue<string>(settings, "preview") ?? string.Empty;
        var subject = GetSettingValue<string>(settings, "subject") ?? string.Empty;
        var listId = GetSettingValue<int>(settings, "list");
        var templateId = GetSettingValue<int?>(settings, "template");

        var sendBody = $@"
<style>
.content {{
  padding: 20px;
  font-size: 15px;
  line-height: 1.6;
}}
</style>
<div class=""hidden-preheader""
       style=""display:none !important; visibility:hidden; opacity:0; overflow:hidden;
              max-height:0; max-width:0; line-height:1px; font-size:1px; color:transparent;
              mso-hide:all;""
>
    {preview}
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
    &zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;&zwj;&nbsp;
  </div>

  <div class=""content"">
    {firstPost.Message}
  </div>
";

        var campaignName = Slugify(subject);

        var campaignPayload = new Dictionary<string, object>
        {
            ["name"] = campaignName,
            ["type"] = "regular",
            ["content_type"] = "html",
            ["subject"] = subject,
            ["lists"] = new[] { listId },
            ["body"] = sendBody
        };

        if (templateId.HasValue && templateId.Value > 0)
        {
            campaignPayload["template_id"] = templateId.Value;
        }

        var campaignRequest = CreateRequest($"{body.Url}/api/campaigns", HttpMethod.Post, campaignPayload);
        campaignRequest.Headers.Add("Accept", "application/json");
        campaignRequest.Headers.Add("Authorization", $"Basic {auth}");

        var campaignResponse = await HttpClient.SendAsync(campaignRequest, cancellationToken);
        campaignResponse.EnsureSuccessStatusCode();

        var campaignData = await DeserializeAsync<ListmonkCampaignResponse>(campaignResponse);
        var postId = campaignData.Data?.Uuid ?? string.Empty;
        var campaignId = campaignData.Data?.Id ?? 0;

        var statusPayload = new { status = "running" };
        var statusRequest = CreateRequest($"{body.Url}/api/campaigns/{campaignId}/status", HttpMethod.Put, statusPayload);
        statusRequest.Headers.Add("Accept", "application/json");
        statusRequest.Headers.Add("Authorization", $"Basic {auth}");

        var statusResponse = await HttpClient.SendAsync(statusRequest, cancellationToken);
        statusResponse.EnsureSuccessStatusCode();

        return new[]
        {
            new PostResponse
            {
                Id = firstPost.Id,
                Status = "completed",
                ReleaseUrl = $"{body.Url}/api/campaigns/{campaignId}/preview",
                PostId = postId
            }
        };
    }

    private ListmonkAuthBody GetAuthBody(Integration integration)
    {
        if (string.IsNullOrEmpty(integration.CustomInstanceDetails))
        {
            throw new InvalidOperationException("CustomInstanceDetails is required for Listmonk");
        }

        var decrypted = Decrypt(integration.CustomInstanceDetails);
        return JsonSerializer.Deserialize<ListmonkAuthBody>(decrypted)
            ?? throw new InvalidOperationException("Failed to parse CustomInstanceDetails");
    }

    private string GetAuthHeader(ListmonkAuthBody body)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{body.Username}:{body.Password}"));
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrEmpty(text)) return "campaign";

        var slug = text.ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-").Trim();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");

        return slug;
    }

    private static T? GetSettingValue<T>(Dictionary<string, object> settings, string key)
    {
        if (!settings.TryGetValue(key, out var value))
            return default;

        if (value is T typedValue)
            return typedValue;

        if (value is JsonElement jsonElement)
        {
            if (typeof(T) == typeof(string))
                return (T)(object)jsonElement.GetString()!;
            if (typeof(T) == typeof(int))
                return (T)(object)jsonElement.GetInt32();
            if (typeof(T) == typeof(int?))
                return (T)(object)jsonElement.GetInt32();
            if (typeof(T) == typeof(bool))
                return (T)(object)jsonElement.GetBoolean();
        }

        var serialized = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<T>(serialized);
    }

    private string Decrypt(string encrypted)
    {
        var key = _configuration["ENCRYPTION_KEY"] ?? "default-encryption-key-32-chars!!";
        var fullCipher = Convert.FromBase64String(encrypted);

        var iv = new byte[16];
        var cipher = new byte[fullCipher.Length - 16];
        Array.Copy(fullCipher, 0, iv, 0, 16);
        Array.Copy(fullCipher, 16, cipher, 0, cipher.Length);

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Take(32).ToArray());
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }

    #region DTOs

    public class ListmonkCustomField
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
        public string Validation { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class ListmonkList
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ListmonkTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class ListmonkAuthBody
    {
        public string Url { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    private class ListmonkSettingsResponse
    {
        [JsonPropertyName("data")]
        public ListmonkSettingsData? Data { get; set; }
    }

    private class ListmonkSettingsData
    {
        [JsonPropertyName("app.site_name")]
        public string? AppSiteName { get; set; }

        [JsonPropertyName("app.logo_url")]
        public string? AppLogoUrl { get; set; }
    }

    private class ListmonkListsResponse
    {
        [JsonPropertyName("data")]
        public ListmonkListsData? Data { get; set; }
    }

    private class ListmonkListsData
    {
        [JsonPropertyName("results")]
        public List<ListmonkListResult>? Results { get; set; }
    }

    private class ListmonkListResult
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private class ListmonkTemplatesResponse
    {
        [JsonPropertyName("data")]
        public List<ListmonkTemplateResult>? Data { get; set; }
    }

    private class ListmonkTemplateResult
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private class ListmonkCampaignResponse
    {
        [JsonPropertyName("data")]
        public ListmonkCampaignData? Data { get; set; }
    }

    private class ListmonkCampaignData
    {
        [JsonPropertyName("uuid")]
        public string? Uuid { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    #endregion
}
