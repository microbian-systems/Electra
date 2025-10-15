// Basic Account Controller tests - main tests are in ElectraAuthIntegrationTests.cs
using System.Net;
using System.Net.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Electra.Auth.Tests;

public class AccountControllerIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public AccountControllerIntegrationTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAccountListPasskeys_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/Account/list");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostLogout_ShouldRequireAntiForgeryToken()
    {
        // Arrange
        var content = new StringContent("", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

        // Act
        var response = await _client.PostAsync("/Account/Logout", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}