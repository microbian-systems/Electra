using System.Text.Json.Serialization;

namespace Electra.Common;

public record AppSettings
{
    public string AppName { get; init; } = string.Empty;
    public string DomainName { get; init; } = string.Empty;
    public string OrganizationName { get; init; } = string.Empty;
    public string AdminEmail { get; init; } = string.Empty;
    public string ReplyToEmail { get; init; } = string.Empty;
    public bool UseProxy { get; init; }
    public bool CloudFlareOnlyConnections { get; init; }
    public ElectraIdentityOptions IdentityOptions { get; init; } = new();
    public List<string> ElasticsearchUrls { get; init; } = [];
    public string AppInsightsKey { get; init; } = string.Empty;
    public bool UseAzureKeyVault { get; init; }
    public string KeyVaultEndPoint { get; init; } = string.Empty;
    public bool EnableHangfire { get; init; }
    public AzureStorageEntry AzureStorage { get; init; } = new();
    public string Secret { get; init; } = string.Empty;
    public AesEncryptionSettings AesEncryptionSettings { get; init; } = new();
    public SendGridSettings SendGrid { get; init; } = new();
    public TwilioSettings Twilio { get; init; } = new();
    public StripeSettings Stripe { get; init; } = new();
    public ZipApiSettings ZipApi { get; init; } = new();
    public bool UseAzureStorage { get; init; }
    public bool UseBlobStorage { get; init; }
    public bool EnableMiniProfiler { get; init; }
    public List<string> ValidIssuers { get; protected init; } = [];
    public bool EnableStaticFileCaching { get; init; }
}

public record AesEncryptionSettings
{
    public string Key { get; set; } = string.Empty;
    public string IV { get; set; } = string.Empty;
}

public record AzureStorageEntry
{
    public string ContainerName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string StorageName { get; set; } = string.Empty;
}

public class ElectraIdentityOptions : BaseOptions
{
    public ElectraIdentityOptions()
    {
        SectionName = nameof(ElectraIdentityOptions);
    }

    public bool RequireConfirmedAccount { get; set; }
    public bool RequireDigit { get; set; }
    public bool RequireLowercase { get; set; }
    public bool RequireNonAlphanumeric { get; set; }
    public bool RequireUppercase { get; set; }
    public int RequiredLength { get; set; }
    public int RequiredUniqueChars { get; set; }
    public bool RequireUniqueEmail { get; set; }
    public int DefaultLockoutTimeSpan { get; set; }
    public int MaxFailedAccessAttempts { get; set; }
    public bool LockoutAllowedForNewUsers { get; set; }

    public string AllowedUserNameCharacters { get; protected set; } =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
}

public record SendGridSettings
{
    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("from")]
    public string From { get; set; }

    [JsonPropertyName("from_name")]
    public string FromName { get; set; }
}

public record StripeSettings
{
    [JsonPropertyName("secret_key")]
    public string SecretKey { get; set; }
}

public record TwilioSettings
{
    [JsonPropertyName("account_sID")]
    public string AccountSid { get; set; }

    [JsonPropertyName("auth_token")]
    public string AuthToken { get; set; }
}

public record ZipApiSettings
{
    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("Password")]
    public string Password { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("ApiKey")]
    public string ApiKey { get; set; }

    [JsonPropertyName("JsApiKey")]
    public string JsApiKey { get; set; }
}