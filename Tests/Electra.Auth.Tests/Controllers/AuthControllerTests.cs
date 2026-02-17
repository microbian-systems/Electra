using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Aero.Auth.Models;

namespace Electra.Auth.Tests.Controllers;

/// <summary>
/// Integration tests for Auth controller endpoints
/// Tests web login, app login, token refresh, and logout flows
/// </summary>
public class AuthControllerIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public AuthControllerIntegrationTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    #region Web Login Tests

    [Fact]
    public async Task LoginWeb_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var request = new LoginWebRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!",
            RememberMe = false
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login-web", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("success");
    }

    [Fact]
    public async Task LoginWeb_WithInvalidEmail_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginWebRequest
        {
            Email = "nonexistent@example.com",
            Password = "TestPassword123!",
            RememberMe = false
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login-web", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginWeb_WithEmptyPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginWebRequest
        {
            Email = "test@example.com",
            Password = "",
            RememberMe = false
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login-web", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginWeb_SetsCookie_WhenSuccessful()
    {
        // Arrange
        var request = new LoginWebRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!",
            RememberMe = false
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login-web", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().Contain(h => 
            h.Key == "Set-Cookie" || h.Key == "set-cookie");
    }

    #endregion

    #region App Login Tests

    [Fact]
    public async Task LoginApp_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var request = new LoginAppRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!",
            ClientType = "mobile"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login-app", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("accessToken");
        body.Should().Contain("refreshToken");
    }

    [Fact]
    public async Task LoginApp_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginAppRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword",
            ClientType = "mobile"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login-app", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginApp_WithNonexistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginAppRequest
        {
            Email = "nonexistent@example.com",
            Password = "TestPassword123!",
            ClientType = "mobile"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login-app", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginApp_ResponseContainsAccessTokenExpiresIn()
    {
        // Arrange
        var request = new LoginAppRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!",
            ClientType = "mobile"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login-app", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("accessTokenExpiresIn");
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_RequiresAuthentication()
    {
        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutApp_WithValidToken_ShouldSucceed()
    {
        // Arrange
        // First login to get a token
        var loginRequest = new LoginAppRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!",
            ClientType = "mobile"
        };

        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        var loginResponse = await _client.PostAsync("/api/auth/login-app", loginContent);
        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<LoginAppResponse>(loginBody);

        // Act
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        var response = await _client.PostAsync("/api/auth/logout-app", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Sessions Tests

    [Fact]
    public async Task GetSessions_RequiresAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeSessions_RequiresAuthentication()
    {
        // Act
        var response = await _client.PostAsync("/api/auth/sessions/session-id/revoke", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Error Response Format Tests

    [Fact]
    public async Task LoginWeb_ErrorResponse_HasCorrectFormat()
    {
        // Arrange
        var request = new LoginWebRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword",
            RememberMe = false
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login-web", content);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        body.Should().Contain("success");
        body.Should().Contain("message");
    }

    [Fact]
    public async Task LoginApp_ErrorResponse_HasCorrectFormat()
    {
        // Arrange
        var request = new LoginAppRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword",
            ClientType = "mobile"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login-app", content);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        body.Should().Contain("success");
        body.Should().Contain("message");
    }

    #endregion

    #region Request Validation Tests

    [Fact]
    public async Task LoginWeb_WithNullRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var content = new StringContent(
            JsonSerializer.Serialize(new { }),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login-web", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LoginApp_WithNullRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var content = new StringContent(
            JsonSerializer.Serialize(new { }),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login-app", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
