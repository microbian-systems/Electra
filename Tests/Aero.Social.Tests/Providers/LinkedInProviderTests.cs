using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class LinkedInProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<LinkedInProvider>> _loggerMock = new();
    
    private LinkedInProvider CreateProvider()
    {
        SetupConfiguration("LINKEDIN_CLIENT_ID", "test_client_id");
        SetupConfiguration("LINKEDIN_CLIENT_SECRET", "test_client_secret");
        SetupConfiguration("FRONTEND_URL", "https://localhost");
        
        return new LinkedInProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();
        
        provider.Identifier.ShouldBe("linkedin");
        provider.Name.ShouldBe("LinkedIn");
        provider.MaxConcurrentJobs.ShouldBe(2);
    }

    [Fact]
    public void MaxLength_ShouldReturn3000()
    {
        var provider = CreateProvider();
        
        provider.MaxLength().ShouldBe(3000);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        var provider = CreateProvider();
        
        var result = await provider.GenerateAuthUrlAsync();
        
        result.Url.ShouldContain("linkedin.com/oauth/v2/authorization");
        result.Url.ShouldContain("client_id=test_client_id");
        result.Url.ShouldContain("redirect_uri=");
        result.Url.ShouldContain("scope=");
        result.State.ShouldNotBeNullOrEmpty();
    }
}
