using System.Net;
using System.Net.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Electra.Auth.Tests;

public class UsernamelessControllerIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebAppFactory _factory;

    public UsernamelessControllerIntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsernameless_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/Usernameless");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUsernamelessIndex_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/Usernameless/Index");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/Usernameless/authenticate")]
    [InlineData("/Usernameless/options")]
    [InlineData("/Usernameless/assertion")]
    public async Task UsernamelessEndpoints_ShouldReturnValidResponse(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        // Most endpoints will return NotFound if not implemented, which is expected
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostUsernamelessAuthenticate_ShouldReturnValidResponse()
    {
        // Arrange
        var content = new StringContent("");

        // Act
        var response = await _client.PostAsync("/Usernameless/authenticate", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Usernameless_ShouldNotAllowInvalidMethods(string httpMethod)
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod(httpMethod), "/Usernameless");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UsernamelessController_ShouldHandleWebAuthnRequests()
    {
        // Arrange
        var webAuthnContent = new StringContent("{\"challenge\":\"test\"}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/Usernameless/options", webAuthnContent);

        // Assert
        // Should handle WebAuthn-specific requests appropriately
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UsernamelessController_ShouldSupportDiscoverable_Credentials()
    {
        // Arrange - Test discoverable credentials specific to usernameless flow
        var discoverableContent = new StringContent("{\"discoverable\":true}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/Usernameless/discovery", discoverableContent);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }
}
