using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class ThreadsProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<ThreadsProvider>> _loggerMock = new();

    private ThreadsProvider CreateProvider()
    {
        SetupConfiguration("THREADS_APP_ID", "test_app_id");
        SetupConfiguration("THREADS_APP_SECRET", "test_app_secret");
        SetupConfiguration("FRONTEND_URL", "https://localhost");

        return new ThreadsProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();

        provider.Identifier.ShouldBe("threads");
        provider.Name.ShouldBe("Threads");
        provider.MaxConcurrentJobs.ShouldBe(2);
    }

    [Fact]
    public void MaxLength_ShouldReturn500()
    {
        var provider = CreateProvider();

        provider.MaxLength().ShouldBe(500);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        var provider = CreateProvider();

        var result = await provider.GenerateAuthUrlAsync();

        result.Url.ShouldContain("threads.net/oauth/authorize");
        result.Url.ShouldContain("client_id=test_app_id");
        result.Url.ShouldContain("redirect_uri=");
        result.Url.ShouldContain("scope=");
        result.State.ShouldNotBeNullOrEmpty();
    }
}
