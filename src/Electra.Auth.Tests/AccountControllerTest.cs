using Electra.Auth.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Electra.Auth.Tests;

// Controllers/AccountControllerTests.cs
using  System.Net;
using System.Threading.Tasks;
using Xunit;

public class AccountControllerTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    
    [Fact]
    public async Task AccountController_List_Passkeys_Success()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var controller = ActivatorUtilities.CreateInstance<AccountController>(services);

        controller.ControllerContext = new()
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = services
            }
        };

        var result = await controller.ListPasskeys(); // whatever action you have
        Assert.NotNull(result);
    }
    
    [Fact]
    public async Task AccountController_Logout_Success()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var controller = ActivatorUtilities.CreateInstance<AccountController>(services);

        controller.ControllerContext = new()
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = services
            }
        };

        var result = await controller.Logout("", CancellationToken.None); // whatever action you have
        Assert.NotNull(result);
    }
    
    [Fact]
    public async Task GetAccount_ReturnsOk()
    {
        // Arrange
        var response = await _client.GetAsync("/api/account");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
