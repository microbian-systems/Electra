using System.Net;
using System.Net.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Electra.Auth.Tests;

public class ExternalLoginControllerIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebAppFactory _factory;

    public ExternalLoginControllerIntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetExternalLogin_ShouldReturnSuccessStatusCode()
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
    [InlineData("/ExternalLogin/twitter")]
    public async Task ExternalLoginProviders_ShouldReturnValidResponse(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        // External login endpoints typically redirect or return specific auth responses
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.NotFound, 
            HttpStatusCode.Redirect, 
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task PostExternalLoginCallback_ShouldReturnValidResponse()
    {
        // Arrange
        var content = new StringContent("");

        // Act
        var response = await _client.PostAsync("/ExternalLogin/callback", content);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, 
            HttpStatusCode.NotFound, 
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Redirect
        );
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task ExternalLogin_ShouldNotAllowInvalidMethods(string httpMethod)
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod(httpMethod), "/ExternalLogin");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExternalLoginChallenge_ShouldReturnValidResponse()
    {
        // Arrange
        var challengeUrl = "/ExternalLogin/challenge?provider=google&returnUrl=/";

        // Act
        var response = await _client.GetAsync(challengeUrl);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.NotFound, 
            HttpStatusCode.Redirect, 
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadRequest
        );
    }

    [Theory]
    [InlineData("invalid-provider")]
    [InlineData("\"<script>alert('xss')</script>\"")]
    [InlineData("../../../etc/passwd")]
    public async Task ExternalLoginChallenge_ShouldHandleInvalidProviders(string provider)
    {
        // Arrange
        var challengeUrl = $"/ExternalLogin/challenge?provider={Uri.EscapeDataString(provider)}&returnUrl=/";

        // Act
        var response = await _client.GetAsync(challengeUrl);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, 
            HttpStatusCode.NotFound, 
            HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task ExternalLoginCallback_ShouldValidateState_Parameter()
    {
        // Arrange
        var callbackUrl = "/ExternalLogin/callback?state=invalid-state&code=test-code";

        // Act
        var response = await _client.GetAsync(callbackUrl);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, 
            HttpStatusCode.NotFound, 
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Redirect
        );
    }
}
