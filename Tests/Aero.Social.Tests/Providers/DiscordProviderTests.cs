using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class DiscordProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<DiscordProvider>> _loggerMock = new();
    
    private DiscordProvider CreateProvider()
    {
        SetupConfiguration("DISCORD_CLIENT_ID", "test_client_id");
        SetupConfiguration("DISCORD_CLIENT_SECRET", "test_client_secret");
        SetupConfiguration("FRONTEND_URL", "https://localhost");
        
        return new DiscordProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();
        
        provider.Identifier.ShouldBe("discord");
        provider.Name.ShouldBe("Discord");
        provider.Scopes.ShouldBe(new[] { "identify", "guilds" });
        provider.Editor.ShouldBe(EditorType.Markdown);
        provider.MaxConcurrentJobs.ShouldBe(5);
    }

    [Fact]
    public void MaxLength_ShouldReturn1980()
    {
        var provider = CreateProvider();
        
        provider.MaxLength().ShouldBe(1980);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        var provider = CreateProvider();
        
        var result = await provider.GenerateAuthUrlAsync();
        
        result.Url.ShouldContain("discord.com/oauth2/authorize");
        result.Url.ShouldContain("client_id=test_client_id");
        result.Url.ShouldContain("redirect_uri=");
        result.Url.ShouldContain("scope=bot+identify+guilds");
        result.State.ShouldNotBeNullOrEmpty();
    }
}
