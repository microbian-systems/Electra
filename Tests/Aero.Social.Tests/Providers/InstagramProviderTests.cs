using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class InstagramProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<InstagramProvider>> _loggerMock = new();
    
    private InstagramProvider CreateProvider()
    {
        SetupConfiguration("FACEBOOK_APP_ID", "test_app_id");
        SetupConfiguration("FACEBOOK_APP_SECRET", "test_app_secret");
        SetupConfiguration("FRONTEND_URL", "https://localhost");
        
        return new InstagramProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();
        
        provider.Identifier.ShouldBe("instagram");
        provider.Name.ShouldBe("Instagram (Facebook Business)");
        provider.IsBetweenSteps.ShouldBeTrue();
        provider.MaxConcurrentJobs.ShouldBe(200);
    }

    [Fact]
    public void MaxLength_ShouldReturn2200()
    {
        var provider = CreateProvider();
        
        provider.MaxLength().ShouldBe(2200);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        var provider = CreateProvider();
        
        var result = await provider.GenerateAuthUrlAsync();
        
        result.Url.ShouldContain("facebook.com");
        result.Url.ShouldContain("client_id=test_app_id");
        result.Url.ShouldContain("redirect_uri=");
        result.Url.ShouldContain("scope=");
        result.State.ShouldNotBeNullOrEmpty();
    }
}
