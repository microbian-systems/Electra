using System.Net;
using System.Text;
using System.Text.Json;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class BlueskyProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<BlueskyProvider>> _loggerMock = new();
    
    private BlueskyProvider CreateProvider()
    {
        SetupConfiguration("FRONTEND_URL", "https://localhost");
        
        return new BlueskyProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();
        
        provider.Identifier.ShouldBe("bluesky");
        provider.Name.ShouldBe("Bluesky");
        provider.Scopes.ShouldBe(new[] { "write:statuses", "profile", "write:media" });
        provider.MaxConcurrentJobs.ShouldBe(2);
    }

    [Fact]
    public void MaxLength_ShouldReturn300()
    {
        var provider = CreateProvider();
        
        provider.MaxLength().ShouldBe(300);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnEmptyUrl()
    {
        var provider = CreateProvider();
        
        var result = await provider.GenerateAuthUrlAsync();
        
        result.Url.ShouldBeEmpty();
        result.State.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnTokenDetails()
    {
        var authBody = new
        {
            service = "https://bsky.social",
            identifier = "testuser.bsky.social",
            password = "app_password"
        };
        var authBodyJson = JsonSerializer.Serialize(authBody);
        var authBodyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(authBodyJson));

        HttpHandler.WhenPost("*createSession*")
            .RespondWith(MockResponses.Bluesky.SessionResponse(
                "did:plc:abc123", 
                "testuser.bsky.social", 
                "access_jwt_token"
            ));
        
        HttpHandler.WhenGet("*getProfile*")
            .RespondWith("{\"did\": \"did:plc:abc123\", \"handle\": \"testuser.bsky.social\", \"displayName\": \"Test User\", \"avatar\": \"https://example.com/avatar.jpg\"}");

        var provider = CreateProvider();
        var parameters = new AuthenticateParams(authBodyBase64, "");
        
        var result = await provider.AuthenticateAsync(parameters);
        
        result.AccessToken.ShouldBe("access_jwt_token");
        result.Id.ShouldBe("did:plc:abc123");
        result.Name.ShouldBe("Test User");
        result.Username.ShouldBe("testuser.bsky.social");
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidCredentials_ShouldThrowException()
    {
        var authBody = new
        {
            service = "https://bsky.social",
            identifier = "testuser.bsky.social",
            password = "wrong_password"
        };
        var authBodyJson = JsonSerializer.Serialize(authBody);
        var authBodyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(authBodyJson));

        HttpHandler.WhenPost("*createSession*")
            .RespondWithStatusCode(HttpStatusCode.Unauthorized);

        var provider = CreateProvider();
        var parameters = new AuthenticateParams(authBodyBase64, "");
        
        await Should.ThrowAsync<BadBodyException>(() => provider.AuthenticateAsync(parameters));
    }
}
