using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class SlackProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<SlackProvider>> _loggerMock = new();

    private SlackProvider CreateProvider()
    {
        SetupConfiguration("SLACK_ID", "test_client_id");
        SetupConfiguration("SLACK_SECRET", "test_client_secret");
        SetupConfiguration("FRONTEND_URL", "https://localhost");

        return new SlackProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();

        provider.Identifier.ShouldBe("slack");
        provider.Name.ShouldBe("Slack");
        provider.MaxConcurrentJobs.ShouldBe(3);
    }

    [Fact]
    public void MaxLength_ShouldReturn400000()
    {
        var provider = CreateProvider();

        provider.MaxLength().ShouldBe(400000);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        var provider = CreateProvider();

        var result = await provider.GenerateAuthUrlAsync();

        result.Url.ShouldContain("slack.com/oauth/v2/authorize");
        result.Url.ShouldContain("client_id=test_client_id");
        result.Url.ShouldContain("redirect_uri=");
        result.Url.ShouldContain("scope=");
        result.State.ShouldNotBeNullOrEmpty();
    }
}
