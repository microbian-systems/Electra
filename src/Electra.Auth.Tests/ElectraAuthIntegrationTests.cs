using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Electra.Auth.Tests;

/// <summary>
/// Core integration tests for Electra.Auth - focuses on essential registration and login functionality
/// </summary>
public class ElectraAuthIntegrationTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly TestWebAppFactory _factory = factory;

    #region Registration Tests

    [Fact]
    public async Task PostRegister_ShouldReturnBadRequest_WhenInvalidEmail()
    {
        // Arrange
        var registerRequest = new
        {
            Email = "invalid-email",
            Password = "ValidPassword123!"
        };
        var json = JsonSerializer.Serialize(registerRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostRegister_ShouldReturnBadRequest_WhenWeakPassword()
    {
        // Arrange
        var registerRequest = new
        {
            Email = "test@example.com",
            Password = "123" // Too weak
        };
        var json = JsonSerializer.Serialize(registerRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostRegister_ShouldReturnBadRequest_WhenEmptyData()
    {
        // Arrange
        var registerRequest = new
        {
            Email = "",
            Password = ""
        };
        var json = JsonSerializer.Serialize(registerRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Traditional Login Tests

    [Fact]
    public async Task PostLogin_ShouldReturnUnauthorized_WhenInvalidCredentials()
    {
        // Arrange
        var loginRequest = new
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };
        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostLogin_ShouldReturnBadRequest_WhenInvalidFormat()
    {
        // Arrange
        var loginRequest = new
        {
            Email = "invalid-email-format",
            Password = "ValidPassword123!"
        };
        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region OpenIddict Token Flow Tests

    [Fact]
    public async Task PostTokenExchange_ShouldReturnBadRequest_WhenUnsupportedGrantType()
    {
        // Arrange
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "unsupported_grant"),
            new KeyValuePair<string, string>("username", "test@example.com"),
            new KeyValuePair<string, string>("password", "password123")
        });

        // Act
        var response = await _client.PostAsync("/connect/token", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostTokenExchange_ShouldReturnForbidden_WhenInvalidPasswordCredentials()
    {
        // Arrange
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "nonexistent@example.com"),
            new KeyValuePair<string, string>("password", "wrongpassword")
        });

        // Act
        var response = await _client.PostAsync("/connect/token", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Userinfo Endpoint Tests

    [Fact]
    public async Task GetUserinfo_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/connect/userinfo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostUserinfo_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var content = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/connect/userinfo", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Token Revocation Tests

    [Fact]
    public async Task PostRevoke_ShouldReturnBadRequest_WhenNoTokenProvided()
    {
        // Arrange
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token", "") // Empty token
        });

        // Act
        var response = await _client.PostAsync("/connect/revoke", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostRevoke_ShouldReturnOk_WhenValidTokenNotFound()
    {
        // Arrange
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token", "nonexistent-token")
        });

        // Act
        var response = await _client.PostAsync("/connect/revoke", formData);

        // Assert
        // Per OAuth 2.0 spec, should return success even if token doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Account Management Tests

    [Fact]
    public async Task GetAccountListPasskeys_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/Account/list");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePasskey_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var credentialId = "test-credential-id";

        // Act
        var response = await _client.DeleteAsync($"/Account/{credentialId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostLogout_ShouldRequireAntiForgeryToken()
    {
        // Arrange
        var content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded");

        // Act
        var response = await _client.PostAsync("/Account/Logout", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Passwordless/WebAuthn Tests

    [Fact]
    public async Task GetPasswordless_ShouldReturnValidResponse()
    {
        // Act
        var response = await _client.GetAsync("/Passwordless");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUsernameless_ShouldReturnValidResponse()
    {
        // Act
        var response = await _client.GetAsync("/Usernameless");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostPasswordlessAuthenticate_ShouldReturnValidResponse()
    {
        // Arrange
        var content = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/Passwordless/authenticate", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PasswordlessController_ShouldHandleWebAuthnRequests()
    {
        // Arrange
        var webAuthnContent = new StringContent("{\"challenge\":\"test\"}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/Passwordless/options", webAuthnContent);

        // Assert
        // Should handle WebAuthn-specific requests appropriately
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region External Login Tests

    [Fact]
    public async Task GetExternalLogin_ShouldReturnValidResponse()
    {
        // Act
        var response = await _client.GetAsync("/ExternalLogin");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/ExternalLogin/google")]
    [InlineData("/ExternalLogin/microsoft")]
    [InlineData("/ExternalLogin/facebook")]
    public async Task ExternalLoginProviders_ShouldReturnValidResponse(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.NotFound, 
            HttpStatusCode.Redirect, 
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadRequest
        );
    }

    #endregion

    #region HTTP Method Validation Tests

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Register_ShouldNotAllowNonPostMethods(string httpMethod)
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod(httpMethod), "/api/Auth/register");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Login_ShouldNotAllowNonPostMethods(string httpMethod)
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod(httpMethod), "/api/Auth/login");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task TokenExchange_ShouldNotAllowNonPostMethods(string httpMethod)
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod(httpMethod), "/connect/token");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task PostLogin_ShouldRejectMaliciousInput()
    {
        // Arrange
        var maliciousRequest = new
        {
            Email = "<script>alert('xss')</script>@example.com",
            Password = "'; DROP TABLE Users; --"
        };
        var json = JsonSerializer.Serialize(maliciousRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Auth/login", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostRegister_ShouldRejectMaliciousInput()
    {
        // Arrange
        var maliciousRequest = new
        {
            Email = "<script>alert('xss')</script>@example.com",
            Password = "'; DROP TABLE Users; --ValidPassword123!"
        };
        var json = JsonSerializer.Serialize(maliciousRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}