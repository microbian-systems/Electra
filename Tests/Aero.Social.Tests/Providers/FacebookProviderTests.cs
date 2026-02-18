using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class FacebookProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<FacebookProvider>> _loggerMock = new();
    
    private FacebookProvider CreateProvider()
    {
        SetupConfiguration("FACEBOOK_APP_ID", "test_app_id");
        SetupConfiguration("FACEBOOK_APP_SECRET", "test_app_secret");
        SetupConfiguration("FRONTEND_URL", "https://localhost");
        
        return new FacebookProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();
        
        provider.Identifier.ShouldBe("facebook");
        provider.Name.ShouldBe("Facebook Page");
        provider.IsBetweenSteps.ShouldBeTrue();
        provider.MaxConcurrentJobs.ShouldBe(100);
    }

    [Fact]
    public void MaxLength_ShouldReturn63206()
    {
        var provider = CreateProvider();
        
        provider.MaxLength().ShouldBe(63206);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        var provider = CreateProvider();
        
        var result = await provider.GenerateAuthUrlAsync();
        
        result.Url.ShouldContain("facebook.com/v20.0/dialog/oauth");
        result.Url.ShouldContain("client_id=test_app_id");
        result.Url.ShouldContain("redirect_uri=");
        result.Url.ShouldContain("state=");
        result.Url.ShouldContain("scope=");
        result.State.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnEmptyToken()
    {
        var provider = CreateProvider();
        
        var result = await provider.RefreshTokenAsync("any_refresh_token");
        
        result.AccessToken.ShouldBeEmpty();
        result.RefreshToken.ShouldBeEmpty();
    }
}
