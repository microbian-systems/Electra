using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Aero.Auth.Tests;

public class RavenDbAuthIntegrationTests(RavenDbAuthWebAppFactory factory) : IClassFixture<RavenDbAuthWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Registration_And_Login_ShouldWork_WithRavenDB()
    {
        var email = $"raven_test_{Guid.NewGuid()}@example.com";
        var password = "Password123!";

        // 1. Register
        var registerRequest = new
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        };
        
        var regResponse = await _client.PostAsJsonAsync("/api/Auth/register", registerRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Login
        var loginRequest = new
        {
            Email = email,
            Password = password
        };
        
        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        loginResult.TryGetProperty("accessToken", out _).Should().BeTrue();
    }
}
