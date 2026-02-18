using Aero.Social.Twitter.Client.Clients;
using Aero.Social.Twitter.Client.Configuration;
using Microsoft.Extensions.Options;

namespace Aero.Social.Twitter.Clients;

public class TwitterClientTests
{
    [Fact]
    public void TwitterClient_ShouldImplementITwitterClient()
    {
        // Arrange
        var httpClient = new HttpClient();
        var options = Options.Create(new TwitterClientOptions
        {
            BearerToken = "test_bearer_token"
        });

        // Act
        var client = new TwitterClient(httpClient, options);

        // Assert
        Assert.IsAssignableFrom<ITwitterClient>(client);
    }

    [Fact]
    public void TwitterClient_Constructor_ShouldThrowOnNullHttpClient()
    {
        // Arrange
        var options = Options.Create(new TwitterClientOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitterClient(null!, options));
    }

    [Fact]
    public void TwitterClient_Constructor_ShouldThrowOnNullOptions()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitterClient(httpClient, null!));
    }
}

public class ITwitterClientTests
{
    [Fact]
    public void ITwitterClient_ShouldHaveGetTweetAsyncMethod()
    {
        // This test verifies the interface contract
        var type = typeof(ITwitterClient);
        var method = type.GetMethod("GetTweetAsync");

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<>).MakeGenericType(typeof(Aero.Social.Twitter.Client.Models.Tweet)), method.ReturnType);
    }
}