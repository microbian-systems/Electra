using System.Security.Cryptography;

namespace Electra.Common.Web.Infrastructure;

/// <summary>
/// Factory for creating API Keys.
/// </summary>
public interface IApiKeyFactory
{
    /// <summary>
    /// Generates an API Key.
    /// </summary>
    /// <returns>The API Key generated.</returns>
    string? GenerateApiKey();
}

/// <summary>
/// Default implementation for the <see cref="IApiKeyFactory"/> service. 
/// Uses <see cref="ApiKeyGenerationOptions"/> to generate keys with the secure <see cref="RandomNumberGenerator"/>.
/// </summary>
public sealed class DefaultApiKeyFactory : IApiKeyFactory
{
    private readonly ApiKeyOptions options;

    public DefaultApiKeyFactory(IOptions<ApiKeyOptions> apiKeyOptions)
    {
        options = apiKeyOptions.Value;
    }

    public string? GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(options.LengthOfKey);

        var base64String = Convert.ToBase64String(bytes);

        if (options.GenerateUrlSafeKeys)
        {
            base64String = base64String
                .Replace("+", "-")
                .Replace("/", "_");
        }
        
        var keyLength = options.LengthOfKey - options.KeyPrefix!.Length;

        return options.KeyPrefix + base64String[..keyLength];
    }
}