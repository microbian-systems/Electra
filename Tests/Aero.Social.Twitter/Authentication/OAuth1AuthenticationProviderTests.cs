using Aero.Social.Twitter.Client.Authentication;
using Aero.Social.Twitter.Client.Configuration;

namespace Aero.Social.Twitter.Authentication;

public class OAuth1AuthenticationProviderTests
{
    [Fact]
    public void Constructor_ShouldThrowOnNullOptions()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OAuth1AuthenticationProvider(null!));
    }

    [Fact]
    public void Constructor_ShouldThrowOnMissingConsumerKey()
    {
        // Arrange
        var options = new TwitterClientOptions
        {
            ConsumerSecret = "secret",
            AccessToken = "token",
            AccessTokenSecret = "token_secret"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new OAuth1AuthenticationProvider(options));
    }

    [Fact]
    public void Constructor_ShouldThrowOnMissingConsumerSecret()
    {
        // Arrange
        var options = new TwitterClientOptions
        {
            ConsumerKey = "key",
            AccessToken = "token",
            AccessTokenSecret = "token_secret"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new OAuth1AuthenticationProvider(options));
    }

    [Fact]
    public void Constructor_ShouldThrowOnMissingAccessToken()
    {
        // Arrange
        var options = new TwitterClientOptions
        {
            ConsumerKey = "key",
            ConsumerSecret = "secret",
            AccessTokenSecret = "token_secret"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new OAuth1AuthenticationProvider(options));
    }

    [Fact]
    public void Constructor_ShouldThrowOnMissingAccessTokenSecret()
    {
        // Arrange
        var options = new TwitterClientOptions
        {
            ConsumerKey = "key",
            ConsumerSecret = "secret",
            AccessToken = "token"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new OAuth1AuthenticationProvider(options));
    }

    [Fact]
    public async Task AuthenticateRequestAsync_ShouldAddAuthorizationHeader()
    {
        // Arrange
        var options = new TwitterClientOptions
        {
            ConsumerKey = "test_consumer_key",
            ConsumerSecret = "test_consumer_secret",
            AccessToken = "test_access_token",
            AccessTokenSecret = "test_access_token_secret"
        };
        var provider = new OAuth1AuthenticationProvider(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitter.com/2/tweets/123");

        // Act
        await provider.AuthenticateRequestAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("OAuth", request.Headers.Authorization.Scheme);
        Assert.NotNull(request.Headers.Authorization.Parameter);
        Assert.Contains("oauth_consumer_key=\"test_consumer_key\"", request.Headers.Authorization.Parameter);
        Assert.Contains("oauth_token=\"test_access_token\"", request.Headers.Authorization.Parameter);
        Assert.Contains("oauth_signature_method=\"HMAC-SHA1\"", request.Headers.Authorization.Parameter);
        Assert.Contains("oauth_version=\"1.0\"", request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_ShouldThrowOnNullRequest()
    {
        // Arrange
        var options = new TwitterClientOptions
        {
            ConsumerKey = "key",
            ConsumerSecret = "secret",
            AccessToken = "token",
            AccessTokenSecret = "token_secret"
        };
        var provider = new OAuth1AuthenticationProvider(options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.AuthenticateRequestAsync(null!, CancellationToken.None));
    }
}