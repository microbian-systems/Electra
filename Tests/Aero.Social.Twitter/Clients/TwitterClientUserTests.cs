using System.Net;
using Aero.Social.Twitter.Client.Clients;
using Aero.Social.Twitter.Client.Configuration;
using Aero.Social.Twitter.Client.Exceptions;
using Aero.Social.Twitter.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Aero.Social.Twitter.Clients;

public class TwitterClientUserTests
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<TwitterClientOptions> _options;
    private readonly ILogger<TwitterClient> _logger;

    public TwitterClientUserTests()
    {
        _httpClient = new HttpClient();
        _options = Options.Create(new TwitterClientOptions
        {
            BearerToken = "test_bearer_token"
        });
        _logger = Substitute.For<ILogger<TwitterClient>>();
    }

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(request =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{
                        ""data"": {
                            ""id"": ""1234567890"",
                            ""name"": ""Test User"",
                            ""username"": ""testuser"",
                            ""created_at"": ""2020-01-01T00:00:00.000Z"",
                            ""verified"": true
                        }
                    }")
            };
            return Task.FromResult(response);
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.twitter.com")
        };

        var twitterClient = new TwitterClient(client, _options, _logger);

        // Act
        var user = await twitterClient.GetUserByIdAsync("1234567890");

        // Assert
        Assert.NotNull(user);
        Assert.Equal("1234567890", user.Id);
        Assert.Equal("Test User", user.Name);
        Assert.Equal("testuser", user.Username);
        Assert.True(user.Verified);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithFields_IncludesFieldsInRequest()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHttpMessageHandler(request =>
        {
            capturedRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{
                        ""data"": {
                            ""id"": ""1234567890"",
                            ""name"": ""Test User"",
                            ""username"": ""testuser"",
                            ""description"": ""Test description"",
                            ""location"": ""Test Location""
                        }
                    }")
            };
            return Task.FromResult(response);
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.twitter.com")
        };

        var twitterClient = new TwitterClient(client, _options, _logger);

        // Act
        await twitterClient.GetUserByIdAsync("1234567890", UserFields.Description | UserFields.Location);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Contains("user.fields", capturedRequest.RequestUri?.Query);
        Assert.Contains("description", capturedRequest.RequestUri?.Query);
        Assert.Contains("location", capturedRequest.RequestUri?.Query);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNullUserId_ThrowsArgumentException()
    {
        // Arrange
        var twitterClient = new TwitterClient(_httpClient, _options, _logger);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            twitterClient.GetUserByIdAsync(null!));
        Assert.Contains("User ID cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithEmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var twitterClient = new TwitterClient(_httpClient, _options, _logger);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            twitterClient.GetUserByIdAsync(""));
        Assert.Contains("User ID cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNotFound_ThrowsTwitterApiException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(request =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(@"{
                        ""errors"": [{
                            ""message"": ""User not found"",
                            ""code"": 50
                        }]
                    }")
            };
            return Task.FromResult(response);
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.twitter.com")
        };

        var twitterClient = new TwitterClient(client, _options, _logger);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TwitterApiException>(() => 
            twitterClient.GetUserByIdAsync("nonexistent"));
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithValidUsername_ReturnsUser()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(request =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{
                        ""data"": {
                            ""id"": ""1234567890"",
                            ""name"": ""Test User"",
                            ""username"": ""testuser"",
                            ""created_at"": ""2020-01-01T00:00:00.000Z""
                        }
                    }")
            };
            return Task.FromResult(response);
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.twitter.com")
        };

        var twitterClient = new TwitterClient(client, _options, _logger);

        // Act
        var user = await twitterClient.GetUserByUsernameAsync("testuser");

        // Assert
        Assert.NotNull(user);
        Assert.Equal("1234567890", user.Id);
        Assert.Equal("testuser", user.Username);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithAtPrefix_RemovesPrefix()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHttpMessageHandler(request =>
        {
            capturedRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{
                        ""data"": {
                            ""id"": ""1234567890"",
                            ""name"": ""Test User"",
                            ""username"": ""testuser""
                        }
                    }")
            };
            return Task.FromResult(response);
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.twitter.com")
        };

        var twitterClient = new TwitterClient(client, _options, _logger);

        // Act
        await twitterClient.GetUserByUsernameAsync("@testuser");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Contains("/by/username/testuser", capturedRequest.RequestUri?.AbsolutePath);
        Assert.DoesNotContain("@", capturedRequest.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithFields_IncludesFieldsInRequest()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHttpMessageHandler(request =>
        {
            capturedRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{
                        ""data"": {
                            ""id"": ""1234567890"",
                            ""name"": ""Test User"",
                            ""username"": ""testuser"",
                            ""public_metrics"": {
                                ""followers_count"": 100,
                                ""following_count"": 50
                            }
                        }
                    }")
            };
            return Task.FromResult(response);
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.twitter.com")
        };

        var twitterClient = new TwitterClient(client, _options, _logger);

        // Act
        await twitterClient.GetUserByUsernameAsync("testuser", UserFields.PublicMetrics);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Contains("user.fields", capturedRequest.RequestUri?.Query);
        Assert.Contains("public_metrics", capturedRequest.RequestUri?.Query);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithNullUsername_ThrowsArgumentException()
    {
        // Arrange
        var twitterClient = new TwitterClient(_httpClient, _options, _logger);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            twitterClient.GetUserByUsernameAsync(null!));
        Assert.Contains("Username cannot be null or empty", exception.Message);
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await _handler(request);
        }
    }
}