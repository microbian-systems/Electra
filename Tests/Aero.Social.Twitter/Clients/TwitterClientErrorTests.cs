using Aero.Social.Twitter.Client.Clients;
using Aero.Social.Twitter.Client.Configuration;
using Microsoft.Extensions.Options;

namespace Aero.Social.Twitter.Clients;

public class TwitterClientErrorTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenNoCredentialsProvided()
    {
        // Arrange
        var httpClient = new HttpClient();
        var options = Options.Create(new TwitterClientOptions());

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new TwitterClient(httpClient, options));
        Assert.Contains("No authentication credentials configured", exception.Message);
    }

    [Fact]
    public async Task GetTweetAsync_ShouldThrowArgumentException_WhenTweetIdIsNull()
    {
        // Arrange
        var httpClient = new HttpClient();
        var options = Options.Create(new TwitterClientOptions { BearerToken = "test" });
        var client = new TwitterClient(httpClient, options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetTweetAsync(null!));
    }

    [Fact]
    public async Task GetTweetAsync_ShouldThrowArgumentException_WhenTweetIdIsEmpty()
    {
        // Arrange
        var httpClient = new HttpClient();
        var options = Options.Create(new TwitterClientOptions { BearerToken = "test" });
        var client = new TwitterClient(httpClient, options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetTweetAsync(""));
    }
}