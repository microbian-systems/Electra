using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class YouTubeProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<YouTubeProvider>> _loggerMock = new();
    
    private YouTubeProvider CreateProvider()
    {
        SetupConfiguration("YOUTUBE_CLIENT_ID", "test_client_id");
        SetupConfiguration("YOUTUBE_CLIENT_SECRET", "test_client_secret");
        SetupConfiguration("FRONTEND_URL", "https://localhost");
        
        return new YouTubeProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();
        
        provider.Identifier.ShouldBe("youtube");
        provider.Name.ShouldBe("YouTube");
        provider.MaxConcurrentJobs.ShouldBe(200);
    }

    [Fact]
    public void MaxLength_ShouldReturn5000()
    {
        var provider = CreateProvider();
        
        provider.MaxLength().ShouldBe(5000);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        var provider = CreateProvider();
        
        var result = await provider.GenerateAuthUrlAsync();
        
        result.Url.ShouldContain("accounts.google.com/o/oauth2/v2/auth");
        result.Url.ShouldContain("client_id=test_client_id");
        result.Url.ShouldContain("redirect_uri=");
        result.Url.ShouldContain("scope=");
        result.Url.ShouldContain("youtube.upload");
        result.State.ShouldNotBeNullOrEmpty();
    }
}
