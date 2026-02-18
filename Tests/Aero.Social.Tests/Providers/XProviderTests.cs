using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class XProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<XProvider>> _loggerMock = new();
    
    private XProvider CreateProvider()
    {
        SetupConfiguration("X_API_KEY", "test_api_key");
        SetupConfiguration("X_API_SECRET", "test_api_secret");
        SetupConfiguration("FRONTEND_URL", "https://localhost");
        
        return new XProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();
        
        provider.Identifier.ShouldBe("x");
        provider.Name.ShouldBe("X");
        provider.Scopes.ShouldBeEmpty();
        provider.MaxConcurrentJobs.ShouldBe(1);
    }

    [Fact]
    public void MaxLength_ShouldReturn200ForFree()
    {
        var provider = CreateProvider();
        
        provider.MaxLength().ShouldBe(200);
    }

    [Fact]
    public void MaxLength_ShouldReturn4000ForPremium()
    {
        var provider = CreateProvider();
        
        provider.MaxLength(true).ShouldBe(4000);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        HttpHandler.WhenPost("*request_token*")
            .RespondWith("oauth_token=request_token_value&oauth_token_secret=request_token_secret&oauth_callback_confirmed=true");

        var provider = CreateProvider();
        
        var result = await provider.GenerateAuthUrlAsync();
        
        result.Url.ShouldContain("api.twitter.com/oauth/authenticate");
        result.Url.ShouldContain("oauth_token=request_token_value");
        result.CodeVerifier.ShouldContain("request_token_value");
        result.State.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnTokenDetails()
    {
        HttpHandler.WhenPost("*access_token*")
            .RespondWith("oauth_token=access_token_value&oauth_token_secret=access_token_secret&user_id=123456&screen_name=testuser");
        
        HttpHandler.WhenGet("*users/me*")
            .RespondWith("{\"data\": {\"id\": \"123456\", \"name\": \"Test User\", \"username\": \"testuser\"}}");

        var provider = CreateProvider();
        var parameters = new AuthenticateParams("oauth_verifier", "request_token:request_token_secret");
        
        var result = await provider.AuthenticateAsync(parameters);
        
        result.AccessToken.ShouldBe("access_token_value:access_token_secret");
        result.Id.ShouldBe("123456");
        result.Name.ShouldBe("Test User");
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
