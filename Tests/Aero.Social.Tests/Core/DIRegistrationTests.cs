using Aero.Social;
using Aero.Social.Abstractions;
using Aero.Social.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Aero.Social.Tests.Core;

public class DIRegistrationTests
{
    private readonly IServiceProvider _serviceProvider;

    public DIRegistrationTests()
    {
        var services = new ServiceCollection();

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["DISCORD_CLIENT_ID"]).Returns("test");
        configMock.Setup(c => c["DISCORD_CLIENT_SECRET"]).Returns("test");
        configMock.Setup(c => c["SLACK_ID"]).Returns("test");
        configMock.Setup(c => c["SLACK_SECRET"]).Returns("test");
        configMock.Setup(c => c["TELEGRAM_TOKEN"]).Returns("test");
        configMock.Setup(c => c["LINKEDIN_CLIENT_ID"]).Returns("test");
        configMock.Setup(c => c["LINKEDIN_CLIENT_SECRET"]).Returns("test");
        configMock.Setup(c => c["FACEBOOK_APP_ID"]).Returns("test");
        configMock.Setup(c => c["FACEBOOK_APP_SECRET"]).Returns("test");
        configMock.Setup(c => c["X_API_KEY"]).Returns("test");
        configMock.Setup(c => c["X_API_SECRET"]).Returns("test");
        configMock.Setup(c => c["REDDIT_CLIENT_ID"]).Returns("test");
        configMock.Setup(c => c["REDDIT_CLIENT_SECRET"]).Returns("test");
        configMock.Setup(c => c["FACEBOOK_APP_ID"]).Returns("test");
        configMock.Setup(c => c["FACEBOOK_APP_SECRET"]).Returns("test");
        configMock.Setup(c => c["TIKTOK_CLIENT_ID"]).Returns("test");
        configMock.Setup(c => c["TIKTOK_CLIENT_SECRET"]).Returns("test");
        configMock.Setup(c => c["YOUTUBE_CLIENT_ID"]).Returns("test");
        configMock.Setup(c => c["YOUTUBE_CLIENT_SECRET"]).Returns("test");
        configMock.Setup(c => c["THREADS_APP_ID"]).Returns("test");
        configMock.Setup(c => c["THREADS_APP_SECRET"]).Returns("test");
        configMock.Setup(c => c["MASTODON_CLIENT_ID"]).Returns("test");
        configMock.Setup(c => c["PINTEREST_CLIENT_ID"]).Returns("test");
        configMock.Setup(c => c["PINTEREST_CLIENT_SECRET"]).Returns("test");
        configMock.Setup(c => c["TWITCH_CLIENT_ID"]).Returns("test");
        configMock.Setup(c => c["TWITCH_CLIENT_SECRET"]).Returns("test");
        configMock.Setup(c => c["DRIBBBLE_CLIENT_ID"]).Returns("test");
        configMock.Setup(c => c["DRIBBBLE_CLIENT_SECRET"]).Returns("test");
        configMock.Setup(c => c["KICK_CLIENT_ID"]).Returns("test");
        configMock.Setup(c => c["KICK_CLIENT_SECRET"]).Returns("test");
        configMock.Setup(c => c["GOOGLE_GMB_CLIENT_ID"]).Returns("test");
        configMock.Setup(c => c["GOOGLE_GMB_CLIENT_SECRET"]).Returns("test");
        configMock.Setup(c => c["INSTAGRAM_APP_ID"]).Returns("test");
        configMock.Setup(c => c["INSTAGRAM_APP_SECRET"]).Returns("test");
        configMock.Setup(c => c["VK_ID"]).Returns("test");
        configMock.Setup(c => c["NEYNAR_CLIENT_ID"]).Returns("test");
        configMock.Setup(c => c["NEYNAR_CLIENT_SECRET"]).Returns("test");
        configMock.Setup(c => c["FRONTEND_URL"]).Returns("https://localhost");

        services.AddSingleton<IConfiguration>(configMock.Object);
        services.AddSocialProviders();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void AllProviders_ShouldBeRegistered()
    {
        var providers = _serviceProvider.GetServices<ISocialProvider>().ToList();

        providers.Count.ShouldBe(29);
    }

    [Fact]
    public void IntegrationManager_ShouldResolveAllProviders()
    {
        var manager = _serviceProvider.GetRequiredService<IntegrationManager>();

        var identifiers = manager.GetAllowedSocialIntegrations().ToList();

        identifiers.Count.ShouldBe(29);
    }

    [Theory]
    [InlineData("discord")]
    [InlineData("slack")]
    [InlineData("telegram")]
    [InlineData("medium")]
    [InlineData("linkedin")]
    [InlineData("facebook")]
    [InlineData("x")]
    [InlineData("reddit")]
    [InlineData("instagram")]
    [InlineData("tiktok")]
    [InlineData("youtube")]
    [InlineData("threads")]
    [InlineData("bluesky")]
    [InlineData("mastodon")]
    [InlineData("pinterest")]
    [InlineData("lemmy")]
    [InlineData("nostr")]
    [InlineData("devto")]
    [InlineData("hashnode")]
    [InlineData("wordpress")]
    [InlineData("twitch")]
    [InlineData("dribbble")]
    [InlineData("kick")]
    [InlineData("wrapcast")]
    [InlineData("gmb")]
    [InlineData("linkedin-page")]
    [InlineData("instagram-standalone")]
    [InlineData("listmonk")]
    [InlineData("vk")]
    public void IntegrationManager_ShouldResolveProvider(string identifier)
    {
        var manager = _serviceProvider.GetRequiredService<IntegrationManager>();

        var provider = manager.GetSocialIntegration(identifier);

        provider.ShouldNotBeNull();
        provider.Identifier.ShouldBe(identifier);
    }

    [Fact]
    public void IntegrationManager_GetAllIntegrationsAsync_ShouldReturnAllProviders()
    {
        var manager = _serviceProvider.GetRequiredService<IntegrationManager>();

        var integrations = manager.GetAllIntegrationsAsync().GetAwaiter().GetResult();

        integrations.Count.ShouldBe(29);
        integrations.Select(i => i.Identifier).Distinct().Count().ShouldBe(29);
    }
}
