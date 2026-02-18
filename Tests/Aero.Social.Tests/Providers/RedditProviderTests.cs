using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class RedditProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<RedditProvider>> _loggerMock = new();

    private RedditProvider CreateProvider()
    {
        SetupConfiguration("REDDIT_CLIENT_ID", "test_client_id");
        SetupConfiguration("REDDIT_CLIENT_SECRET", "test_client_secret");
        SetupConfiguration("FRONTEND_URL", "https://localhost");

        return new RedditProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();

        provider.Identifier.ShouldBe("reddit");
        provider.Name.ShouldBe("Reddit");
        provider.Scopes.ShouldBe(new[] { "read", "identity", "submit", "flair" });
        provider.MaxConcurrentJobs.ShouldBe(1);
    }

    [Fact]
    public void MaxLength_ShouldReturn10000()
    {
        var provider = CreateProvider();

        provider.MaxLength().ShouldBe(10000);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        var provider = CreateProvider();

        var result = await provider.GenerateAuthUrlAsync();

        result.Url.ShouldContain("reddit.com/api/v1/authorize");
        result.Url.ShouldContain("client_id=test_client_id");
        result.Url.ShouldContain("redirect_uri=");
        result.Url.ShouldContain("duration=permanent");
        result.State.ShouldNotBeNullOrEmpty();
    }
}
