using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class DribbbleProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<DribbbleProvider>> _loggerMock = new();

    private DribbbleProvider CreateProvider()
    {
        SetupConfiguration("DRIBBBLE_CLIENT_ID", "test_client_id");
        SetupConfiguration("DRIBBBLE_CLIENT_SECRET", "test_client_secret");
        SetupConfiguration("FRONTEND_URL", "https://localhost");

        return new DribbbleProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();

        provider.Identifier.ShouldBe("dribbble");
        provider.Name.ShouldBe("Dribbble");
        provider.MaxConcurrentJobs.ShouldBe(3);
    }

    [Fact]
    public void MaxLength_ShouldReturn40000()
    {
        var provider = CreateProvider();

        provider.MaxLength().ShouldBe(40000);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        var provider = CreateProvider();

        var result = await provider.GenerateAuthUrlAsync();

        result.Url.ShouldContain("dribbble.com/oauth/authorize");
        result.Url.ShouldContain("client_id=test_client_id");
        result.Url.ShouldContain("redirect_uri=");
        result.Url.ShouldContain("response_type=code");
        result.State.ShouldNotBeNullOrEmpty();
    }
}
