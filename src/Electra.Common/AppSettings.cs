using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Electra.Common
{
    public class FirewallRules
    {
        public bool EnableFirewall { get; set; } = false;
        public  List<string> AllowedIps { get; } = new();
        public List<string> AllowedCidrs { get; } = new();
        public List<string> AllowedCountries { get; } = new();
    }
    
    public class AzureStorageEntry
    {
        public string ContainerName { get; set; } = string.Empty;
        public string StorageKey { get; set; } = string.Empty;
        public string StorageName { get; set; } = string.Empty;
    }
    
    // todo - remove the connection string as the default for piranha uses the "ConnectionStrings:Default"
    // and not "AppSettings:ConnectionStrings:Default".  Create a new class that is composed of both 
    // AppSettings and ConnectionStrings classes
    public class ConnectionsStrings
    {
        // todo - consider having a ReadonlyDictionary<string, string> to allow for multi db connstrings
        public string Default { get; set; } = string.Empty;
    }

    public class AppxIdentityOptions : BaseOptions
    {
        public AppxIdentityOptions()
        {
            SectionName = nameof(AppxIdentityOptions);
        }
        public bool RequireConfirmedAccount {get; set;}
        public bool RequireDigit {get; set;}
        public bool RequireLowercase {get; set;}
        public bool RequireNonAlphanumeric {get; set;}
        public bool RequireUppercase {get; set;}
        public int RequiredLength {get; set;}
        public int RequiredUniqueChars {get; set;}
        public bool RequireUniqueEmail {get; set;}
        public int DefaultLockoutTimeSpan { get; set; }
        public int MaxFailedAccessAttempts {get; set;}
        public bool LockoutAllowedForNewUsers {get; set;}
        public string AllowedUserNameCharacters { get; protected set; } =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    }
    
    public class SendGridSettings
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("from_name")]
        public string FromName { get; set; }
    }

    public class StripeSettings
    {
        [JsonPropertyName("secret_key")]
        public string SecretKey { get; set; }
    }

    public class TwilioSettings
    {
        [JsonPropertyName("account_sID")]
        public string AccountSid { get; set; }

        [JsonPropertyName("auth_token")]
        public string AuthToken { get; set; }
    }

    public class ZipApiSettings
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
    
    public class AppSettings
    {
        public string AppName { get; set; } = string.Empty;
        public string DomainName { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public bool UseProxy { get; set; }
        public bool CloudFlareOnlyConnections { get; set; } 
        public List<string> ElasticsearchUrls { get; set; } = new();
        public string AppInsightsKey { get; set; } = string.Empty;
        public bool UseAzureKeyVault { get; set; }
        public string KeyVaultEndPoint { get; set; } = string.Empty;
        public bool EnableHangfire { get; set; }
        public AzureStorageEntry AzureStorage { get; set; } = new();
        public FirewallRules FirewallRules { get; set; } = new();
        public string Secret { get; set; } = string.Empty;
        public SendGridSettings SendGrid { get; set; } = new();
        public TwilioSettings Twilio { get; set; } = new();
        public StripeSettings Stripe { get; set; } = new();
        public ZipApiSettings ZipApi { get; set; } = new();
        public bool UseLocalDB { get; set; } = false;
        public bool UseAzureStorage { get; set; } = false;
        public bool UseBlobStorage { get; set; }
        public bool EnableMiniProfiler { get; set; } = false;
        public List<string> ValidIssuers { get; protected set; } = new();
        // todo - remove the Connection strings param from appSettings as it's already wired up in another section
        public ConnectionsStrings ConnStrings { get; } = new();
        public bool EnableStaticFileCaching { get; set; }
    }
}