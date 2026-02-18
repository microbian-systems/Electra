using Aero.Social.Twitter.Client.Clients;
using Aero.Social.Twitter.Client.Configuration;
using Aero.Social.Twitter.Client.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Aero.Social.Twitter.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTwitterClient_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTwitterClient(options =>
        {
            options.BearerToken = "test-bearer-token";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var client = provider.GetService<ITwitterClient>();
        Assert.NotNull(client);
        Assert.IsType<TwitterClient>(client);
    }

    [Fact]
    public void AddTwitterClient_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTwitterClient(options =>
        {
            options.ConsumerKey = "test-key";
            options.ConsumerSecret = "test-secret";
            options.MaxRetries = 5;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TwitterClientOptions>>().Value;
        Assert.Equal("test-key", options.ConsumerKey);
        Assert.Equal("test-secret", options.ConsumerSecret);
        Assert.Equal(5, options.MaxRetries);
    }
}