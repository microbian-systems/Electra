using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class ListmonkProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<ListmonkProvider>> _loggerMock = new();

    private ListmonkProvider CreateProvider()
    {
        return new ListmonkProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();

        provider.Identifier.ShouldBe("listmonk");
        provider.Name.ShouldBe("ListMonk");
        provider.Editor.ShouldBe(EditorType.Html);
        provider.MaxConcurrentJobs.ShouldBe(100);
    }

    [Fact]
    public void MaxLength_ShouldReturn100000000()
    {
        var provider = CreateProvider();

        provider.MaxLength().ShouldBe(100000000);
    }
}
