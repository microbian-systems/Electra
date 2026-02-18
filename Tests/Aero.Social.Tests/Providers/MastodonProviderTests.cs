using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class MastodonProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<MastodonProvider>> _loggerMock = new();
    
    private MastodonProvider CreateProvider()
    {
        SetupConfiguration("MASTODON_CLIENT_ID", "test_client_id");
        SetupConfiguration("MASTODON_CLIENT_SECRET", "test_client_secret");
        SetupConfiguration("FRONTEND_URL", "https://localhost");
        
        return new MastodonProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();
        
        provider.Identifier.ShouldBe("mastodon");
        provider.Name.ShouldBe("Mastodon");
        provider.MaxConcurrentJobs.ShouldBe(5);
    }

    [Fact]
    public void MaxLength_ShouldReturn500()
    {
        var provider = CreateProvider();
        
        provider.MaxLength().ShouldBe(500);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        HttpHandler.WhenGet("*instance*")
            .RespondWith(MockResponses.Mastodon.InstanceResponse("mastodon.social"));

        var provider = CreateProvider();
        var clientInfo = new ClientInformation { InstanceUrl = "https://mastodon.social" };
        
        var result = await provider.GenerateAuthUrlAsync(clientInfo);
        
        result.Url.ShouldContain("mastodon.social/oauth/authorize");
        result.Url.ShouldContain("client_id=");
        result.Url.ShouldContain("redirect_uri=");
        result.State.ShouldNotBeNullOrEmpty();
    }
}
