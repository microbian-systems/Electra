using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class InstagramStandaloneProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<InstagramStandaloneProvider>> _loggerMock = new();

    private InstagramStandaloneProvider CreateProvider()
    {
        SetupConfiguration("INSTAGRAM_APP_ID", "test_client_id");
        SetupConfiguration("INSTAGRAM_APP_SECRET", "test_client_secret");
        SetupConfiguration("FRONTEND_URL", "https://localhost");

        return new InstagramStandaloneProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();

        provider.Identifier.ShouldBe("instagram-standalone");
        provider.Name.ShouldContain("Instagram");
        provider.MaxConcurrentJobs.ShouldBe(200);
        provider.IsBetweenSteps.ShouldBeFalse();
    }

    [Fact]
    public void MaxLength_ShouldReturn2200()
    {
        var provider = CreateProvider();

        provider.MaxLength().ShouldBe(2200);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        var provider = CreateProvider();

        var result = await provider.GenerateAuthUrlAsync();

        result.Url.ShouldContain("instagram.com");
        result.Url.ShouldContain("client_id=test_client_id");
        result.State.ShouldNotBeNullOrEmpty();
    }
}
