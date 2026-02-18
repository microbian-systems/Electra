using Aero.Social.Twitter.Client.Authentication;
using Aero.Social.Twitter.Client.Configuration;

namespace Aero.Social.Twitter.Authentication;

public class OAuth2AuthenticationProviderTests
{
    [Fact]
    public void Constructor_ShouldThrowOnNullOptions()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OAuth2AuthenticationProvider(null!));
    }

    [Fact]
    public void Constructor_ShouldThrowOnMissingBearerToken()
    {
        // Arrange
        var options = new TwitterClientOptions();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new OAuth2AuthenticationProvider(options));
    }

    [Fact]
    public async Task AuthenticateRequestAsync_ShouldAddBearerToken()
    {
        // Arrange
        var options = new TwitterClientOptions
        {
            BearerToken = "test_bearer_token_123"
        };
        var provider = new OAuth2AuthenticationProvider(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitter.com/2/tweets/123");

        // Act
        await provider.AuthenticateRequestAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Equal("test_bearer_token_123", request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_ShouldThrowOnNullRequest()
    {
        // Arrange
        var options = new TwitterClientOptions
        {
            BearerToken = "token"
        };
        var provider = new OAuth2AuthenticationProvider(options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.AuthenticateRequestAsync(null!, CancellationToken.None));
    }
}