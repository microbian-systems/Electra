using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Aero.Social;

public class IntegrationManager
{
    private readonly Dictionary<string, ISocialProvider> _providers;
    private readonly IServiceProvider _serviceProvider;

    public IntegrationManager(IServiceProvider serviceProvider, IEnumerable<ISocialProvider> providers)
    {
        _serviceProvider = serviceProvider;
        _providers = providers.ToDictionary(p => p.Identifier, p => p, StringComparer.OrdinalIgnoreCase);
    }

    public ISocialProvider GetSocialIntegration(string identifier)
    {
        if (_providers.TryGetValue(identifier, out var provider))
        {
            return provider;
        }

        throw new KeyNotFoundException($"Social provider '{identifier}' not found");
    }

    public IEnumerable<string> GetAllowedSocialIntegrations()
    {
        return _providers.Keys;
    }

    public async Task<List<ProviderInfo>> GetAllIntegrationsAsync()
    {
        var result = new List<ProviderInfo>();

        foreach (var provider in _providers.Values)
        {
            var info = new ProviderInfo
            {
                Name = provider.Name,
                Identifier = provider.Identifier,
                Tooltip = provider.Tooltip,
                Editor = provider.Editor.ToString().ToLowerInvariant(),
                IsExternal = false,
                IsWeb3 = provider.IsWeb3,
                CustomFields = null
            };

            result.Add(info);
        }

        return result;
    }

    public Dictionary<string, List<ToolInfo>> GetAllTools()
    {
        return _providers.Values.ToDictionary(
            p => p.Identifier,
            p => new List<ToolInfo>()
        );
    }

    public Dictionary<string, string> GetAllRulesDescriptions()
    {
        return _providers.Values.ToDictionary(
            p => p.Identifier,
            p => string.Empty
        );
    }
}

public record ProviderInfo
{
    public string Name { get; init; } = string.Empty;
    public string Identifier { get; init; } = string.Empty;
    public string? Tooltip { get; init; }
    public string Editor { get; init; } = "normal";
    public bool IsExternal { get; init; }
    public bool IsWeb3 { get; init; }
    public List<CustomField>? CustomFields { get; init; }
}

public record CustomField
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? DefaultValue { get; init; }
    public string Validation { get; init; } = string.Empty;
    public string Type { get; init; } = "text";
}

public record ToolInfo
{
    public string Description { get; init; } = string.Empty;
    public object DataSchema { get; init; } = new();
    public string MethodName { get; init; } = string.Empty;
}

public static class SocialProviderExtensions
{
    public static IServiceCollection AddSocialProviders(this IServiceCollection services)
    {
        services.AddScoped<IntegrationManager>();
        
        services.AddHttpClient<DiscordProvider>();
        services.AddHttpClient<SlackProvider>();
        services.AddHttpClient<TelegramProvider>();
        services.AddHttpClient<MediumProvider>();
        services.AddHttpClient<LinkedInProvider>();
        services.AddHttpClient<FacebookProvider>();
        services.AddHttpClient<XProvider>();
        services.AddHttpClient<RedditProvider>();
        services.AddHttpClient<InstagramProvider>();
        services.AddHttpClient<TikTokProvider>();
        services.AddHttpClient<YouTubeProvider>();
        services.AddHttpClient<PinterestProvider>();
        services.AddHttpClient<ThreadsProvider>();
        services.AddHttpClient<BlueskyProvider>();
        services.AddHttpClient<MastodonProvider>();
        services.AddHttpClient<LemmyProvider>();
        services.AddHttpClient<FarcasterProvider>();
        services.AddHttpClient<NostrProvider>();
        services.AddHttpClient<VkProvider>();
        services.AddHttpClient<DevToProvider>();
        services.AddHttpClient<HashnodeProvider>();
        services.AddHttpClient<WordPressProvider>();
        services.AddHttpClient<ListmonkProvider>();
        services.AddHttpClient<DribbbleProvider>();
        services.AddHttpClient<TwitchProvider>();
        services.AddHttpClient<KickProvider>();
        services.AddHttpClient<GmbProvider>();
        services.AddHttpClient<LinkedInPageProvider>();
        services.AddHttpClient<InstagramStandaloneProvider>();

        services.AddTransient<ISocialProvider, DiscordProvider>();
        services.AddTransient<ISocialProvider, SlackProvider>();
        services.AddTransient<ISocialProvider, TelegramProvider>();
        services.AddTransient<ISocialProvider, MediumProvider>();
        services.AddTransient<ISocialProvider, LinkedInProvider>();
        services.AddTransient<ISocialProvider, FacebookProvider>();
        services.AddTransient<ISocialProvider, XProvider>();
        services.AddTransient<ISocialProvider, RedditProvider>();
        services.AddTransient<ISocialProvider, InstagramProvider>();
        services.AddTransient<ISocialProvider, TikTokProvider>();
        services.AddTransient<ISocialProvider, YouTubeProvider>();
        services.AddTransient<ISocialProvider, PinterestProvider>();
        services.AddTransient<ISocialProvider, ThreadsProvider>();
        services.AddTransient<ISocialProvider, BlueskyProvider>();
        services.AddTransient<ISocialProvider, MastodonProvider>();
        services.AddTransient<ISocialProvider, LemmyProvider>();
        services.AddTransient<ISocialProvider, FarcasterProvider>();
        services.AddTransient<ISocialProvider, NostrProvider>();
        services.AddTransient<ISocialProvider, VkProvider>();
        services.AddTransient<ISocialProvider, DevToProvider>();
        services.AddTransient<ISocialProvider, HashnodeProvider>();
        services.AddTransient<ISocialProvider, WordPressProvider>();
        services.AddTransient<ISocialProvider, ListmonkProvider>();
        services.AddTransient<ISocialProvider, DribbbleProvider>();
        services.AddTransient<ISocialProvider, TwitchProvider>();
        services.AddTransient<ISocialProvider, KickProvider>();
        services.AddTransient<ISocialProvider, GmbProvider>();
        services.AddTransient<ISocialProvider, LinkedInPageProvider>();
        services.AddTransient<ISocialProvider, InstagramStandaloneProvider>();

        return services;
    }
}
