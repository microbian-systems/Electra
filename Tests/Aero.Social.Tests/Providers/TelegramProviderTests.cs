using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Providers;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Providers;

public class TelegramProviderTests : ProviderTestBase
{
    private readonly Mock<ILogger<TelegramProvider>> _loggerMock = new();
    
    private TelegramProvider CreateProvider()
    {
        SetupConfiguration("TELEGRAM_TOKEN", "123456:ABC-DEF");
        
        return new TelegramProvider(HttpClient, ConfigurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Provider_ShouldHaveCorrectIdentifier()
    {
        var provider = CreateProvider();
        
        provider.Identifier.ShouldBe("telegram");
        provider.Name.ShouldBe("Telegram");
        provider.MaxConcurrentJobs.ShouldBe(3);
        provider.OneTimeToken.ShouldBeFalse();
    }

    [Fact]
    public void MaxLength_ShouldReturn4096()
    {
        var provider = CreateProvider();
        
        provider.MaxLength().ShouldBe(4096);
    }
}
