using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class HashnodeProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<HashnodeProvider>> _loggerMock = new();

    private HashnodeProvider CreateProvider()
    {
        return new HashnodeProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();

        provider.Identifier.ShouldBe("hashnode");
        provider.Name.ShouldBe("Hashnode");
        provider.Editor.ShouldBe(EditorType.Markdown);
        provider.MaxConcurrentJobs.ShouldBe(3);
    }

    [Fact]
    public void MaxLength_ShouldReturn10000()
    {
        var provider = CreateProvider();

        provider.MaxLength().ShouldBe(10000);
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
