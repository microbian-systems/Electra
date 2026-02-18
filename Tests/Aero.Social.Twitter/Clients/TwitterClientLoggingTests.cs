using Aero.Social.Twitter.Client.Clients;
using Aero.Social.Twitter.Client.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Aero.Social.Twitter.Clients;

public class TwitterClientLoggingTests
{
    [Fact]
    public void Constructor_WithLogger_ShouldAcceptNullLogger()
    {
        // Arrange
        var httpClient = new HttpClient();
        var options = Options.Create(new TwitterClientOptions
        {
            BearerToken = "test_token"
        });

        // Act - Should not throw
        var client = new TwitterClient(httpClient, options, null);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithLogger_ShouldAcceptLogger()
    {
        // Arrange
        var httpClient = new HttpClient();
        var options = Options.Create(new TwitterClientOptions
        {
            BearerToken = "test_token"
        });
        var logger = Substitute.For<ILogger<TwitterClient>>();

        // Act - Should not throw
        var client = new TwitterClient(httpClient, options, logger);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithLogger_ShouldLogInitialization()
    {
        // Arrange
        var httpClient = new HttpClient { BaseAddress = new System.Uri("https://api.twitter.com") };
        var options = Options.Create(new TwitterClientOptions
        {
            BearerToken = "test_token"
        });
        var logger = Substitute.For<ILogger<TwitterClient>>();

        // Act
        _ = new TwitterClient(httpClient, options, logger);

        // Assert
        logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("TwitterClient initialized")),
            null,
            Arg.Any<System.Func<object, System.Exception?, string>>());
    }

    [Fact]
    public void Constructor_WithLogger_ShouldLogOAuth2ProviderSelection()
    {
        // Arrange
        var httpClient = new HttpClient { BaseAddress = new System.Uri("https://api.twitter.com") };
        var options = Options.Create(new TwitterClientOptions
        {
            BearerToken = "test_token"
        });
        var logger = Substitute.For<ILogger<TwitterClient>>();

        // Act
        _ = new TwitterClient(httpClient, options, logger);

        // Assert
        logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("OAuth 2.0")),
            null,
            Arg.Any<System.Func<object, System.Exception?, string>>());
    }
}