using Aero.Social.Twitter.Client.Configuration;

namespace Aero.Social.Twitter.Configuration;

public class TwitterClientOptionsTests
{
    [Fact]
    public void TwitterClientOptions_ShouldHaveDefaultTimeout()
    {
        // Arrange & Act
        var options = new TwitterClientOptions();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), options.Timeout);
    }

    [Fact]
    public void TwitterClientOptions_ShouldHaveDefaultMaxRetries()
    {
        // Arrange & Act
        var options = new TwitterClientOptions();

        // Assert
        Assert.Equal(3, options.MaxRetries);
    }

    [Fact]
    public void TwitterClientOptions_ShouldAllowSettingCredentials()
    {
        // Arrange
        var options = new TwitterClientOptions
        {
            ConsumerKey = "test-consumer-key",
            ConsumerSecret = "test-consumer-secret",
            AccessToken = "test-access-token",
            AccessTokenSecret = "test-access-token-secret",
            BearerToken = "test-bearer-token"
        };

        // Assert
        Assert.Equal("test-consumer-key", options.ConsumerKey);
        Assert.Equal("test-consumer-secret", options.ConsumerSecret);
        Assert.Equal("test-access-token", options.AccessToken);
        Assert.Equal("test-access-token-secret", options.AccessTokenSecret);
        Assert.Equal("test-bearer-token", options.BearerToken);
    }

    [Fact]
    public void TwitterClientOptions_ShouldAllowCustomizingTimeout()
    {
        // Arrange
        var customTimeout = TimeSpan.FromSeconds(60);

        // Act
        var options = new TwitterClientOptions
        {
            Timeout = customTimeout
        };

        // Assert
        Assert.Equal(customTimeout, options.Timeout);
    }

    [Fact]
    public void TwitterClientOptions_ShouldAllowCustomizingMaxRetries()
    {
        // Arrange
        var customRetries = 5;

        // Act
        var options = new TwitterClientOptions
        {
            MaxRetries = customRetries
        };

        // Assert
        Assert.Equal(customRetries, options.MaxRetries);
    }
}