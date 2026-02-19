using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Services;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Shared;

public class EnvironmentKeyVaultServiceTests
{
    [Fact]
    public async Task GetSecretAsync_WithKnownKey_ReturnsValueFromConfiguration()
    {
        var config = Substitute.For<IConfiguration>();
        config["MySecret"].Returns("secret-value");
        var service = new EnvironmentKeyVaultService(config);
        
        var result = await service.GetSecretAsync("MySecret");
        
        result.ShouldBe("secret-value");
    }

    [Fact]
    public async Task GetSecretAsync_WithUnknownKey_ReturnsNull()
    {
        var config = Substitute.For<IConfiguration>();
        config["UnknownKey"].Returns((string?)null);
        var service = new EnvironmentKeyVaultService(config);
        
        var result = await service.GetSecretAsync("UnknownKey");
        
        result.ShouldBeNull();
    }

    [Fact]
    public async Task IKeyVaultService_CanBeSubstitutedWithNSubstitute()
    {
        var substitute = Substitute.For<IKeyVaultService>();
        substitute.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((string?)"value"));
        
        var result = await substitute.GetSecretAsync("key");
        
        result.ShouldBe("value");
    }
}
