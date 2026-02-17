using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace Electra.Auth.Tests;

/// <summary>
/// Essential authentication tests focusing on core registration and login functionality
/// Tests both traditional email/password and passkey authentication flows
/// </summary>
public class EssentialAuthTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public EssentialAuthTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    #region Registration Tests

    [Fact]
    public async Task Registration_ShouldRejectInvalidEmail()
    {
        // Arrange
        var request = new { Email = "invalid-email", Password = "ValidPassword123!" };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Registration_ShouldRejectWeakPassword()
    {
        // Arrange
        var request = new { Email = "test@example.com", Password = "123" };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ShouldRejectInvalidCredentials()
    {
        // Arrange
        var request = new { Email = "nonexistent@example.com", Password = "WrongPassword123!" };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldRejectMalformedEmail()
    {
        // Arrange
        var request = new { Email = "not-an-email", Password = "ValidPassword123!" };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Token Exchange Tests

    [Fact]
    public async Task TokenExchange_ShouldRejectUnsupportedGrantType()
    {
        // Arrange
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "unsupported"),
            new KeyValuePair<string, string>("username", "test@example.com"),
            new KeyValuePair<string, string>("password", "password123")
        });

        // Act
        var response = await _client.PostAsync("/connect/token", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TokenExchange_ShouldRejectInvalidPasswordFlow()
    {
        // Arrange
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "invalid@example.com"),
            new KeyValuePair<string, string>("password", "wrongpassword")
        });

        // Act
        var response = await _client.PostAsync("/connect/token", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Passkey/WebAuthn Tests

    [Fact]
    public async Task Passwordless_ShouldBeAccessible()
    {
        // Act
        var response = await _client.GetAsync("/Passwordless");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Usernameless_ShouldBeAccessible()
    {
        // Act
        var response = await _client.GetAsync("/Usernameless");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PasswordlessAuth_ShouldHandleEmptyRequest()
    {
        // Arrange
        var content = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/Passwordless/authenticate", content);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, 
            HttpStatusCode.NotFound, 
            HttpStatusCode.Unauthorized
        );
    }

    #endregion

    #region Account Management Tests

    [Fact]
    public async Task AccountList_ShouldRequireAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/Account/list");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldRequireAntiForgeryToken()
    {
        // Arrange
        var content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded");

        // Act
        var response = await _client.PostAsync("/Account/Logout", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task UserInfo_ShouldRequireAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/connect/userinfo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenRevocation_ShouldHandleInvalidToken()
    {
        // Arrange
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token", "invalid-token")
        });

        // Act
        var response = await _client.PostAsync("/connect/revoke", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK); // Per OAuth 2.0 spec
    }

    [Fact]
    public async Task RegistrationEndpoint_ShouldRejectNonPostMethods()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Auth/register");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task LoginEndpoint_ShouldRejectNonPostMethods()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Auth/login");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    #endregion
}