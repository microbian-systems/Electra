using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class NostrProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<NostrProvider>> _loggerMock = new();

    private NostrProvider CreateProvider()
    {
        return new NostrProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();

        provider.Identifier.ShouldBe("nostr");
        provider.Name.ShouldBe("Nostr");
        provider.IsWeb3.ShouldBeTrue();
        provider.MaxConcurrentJobs.ShouldBe(5);
    }

    [Fact]
    public void MaxLength_ShouldReturn100000()
    {
        var provider = CreateProvider();

        provider.MaxLength().ShouldBe(100000);
    }

    [Fact]
    public async Task GenerateAuthUrlAsync_ShouldReturnEmptyUrl()
    {
        var provider = CreateProvider();

        var result = await provider.GenerateAuthUrlAsync();

        result.Url.ShouldBeEmpty();
        result.State.ShouldNotBeNullOrEmpty();
    }
}
