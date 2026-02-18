using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class GmbProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<GmbProvider>> _loggerMock = new();

    private GmbProvider CreateProvider()
    {
        SetupConfiguration("GOOGLE_GMB_CLIENT_ID", "test_client_id");
        SetupConfiguration("GOOGLE_GMB_CLIENT_SECRET", "test_client_secret");
        SetupConfiguration("FRONTEND_URL", "https://localhost");

        return new GmbProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();

        provider.Identifier.ShouldBe("gmb");
        provider.Name.ShouldBe("Google My Business");
        provider.IsBetweenSteps.ShouldBeTrue();
        provider.MaxConcurrentJobs.ShouldBe(3);
    }

    [Fact]
    public void MaxLength_ShouldReturn1500()
    {
        var provider = CreateProvider();

        provider.MaxLength().ShouldBe(1500);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        var provider = CreateProvider();

        var result = await provider.GenerateAuthUrlAsync();

        result.Url.ShouldContain("accounts.google.com");
        result.Url.ShouldContain("client_id=test_client_id");
        result.Url.ShouldContain("redirect_uri=");
        result.State.ShouldNotBeNullOrEmpty();
    }
}
