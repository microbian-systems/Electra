using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class TikTokProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<TikTokProvider>> _loggerMock = new();

    private TikTokProvider CreateProvider()
    {
        SetupConfiguration("TIKTOK_CLIENT_ID", "test_client_id");
        SetupConfiguration("TIKTOK_CLIENT_SECRET", "test_client_secret");
        SetupConfiguration("FRONTEND_URL", "https://localhost");

        return new TikTokProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();

        provider.Identifier.ShouldBe("tiktok");
        provider.Name.ShouldBe("TikTok");
        provider.ConvertToJpeg.ShouldBeTrue();
        provider.MaxConcurrentJobs.ShouldBe(1);
    }

    [Fact]
    public void MaxLength_ShouldReturn2000()
    {
        var provider = CreateProvider();

        provider.MaxLength().ShouldBe(2000);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnValidUrl()
    {
        var provider = CreateProvider();

        var result = await provider.GenerateAuthUrlAsync();

        result.Url.ShouldContain("tiktok.com/v2/auth/authorize");
        result.Url.ShouldContain("client_key=test_client_id");
        result.Url.ShouldContain("redirect_uri=");
        result.Url.ShouldContain("response_type=code");
        result.State.ShouldNotBeNullOrEmpty();
    }
}
