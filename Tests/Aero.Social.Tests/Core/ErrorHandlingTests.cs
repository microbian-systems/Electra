using System.Net;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Core;

public class ErrorHandlingTests : ProviderTestBase
{
    [Fact]
    public async Task FetchWithRetryAsync_OnSuccess_ShouldReturnResponse()
    {
        HttpHandler.WhenPost("*")
            .RespondWith("{\"id\": \"123\"}", HttpStatusCode.OK);

        var provider = new TestErrorHandlingProvider(HttpClient, LoggerMock.Object);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/post");
        
        var response = await provider.TestFetchWithRetryAsync("https://api.test.com/post", request);
        
        response.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact]
    public async Task FetchWithRetryAsync_OnTooManyRequests_ShouldRetry()
    {
        var callCount = 0;
        HttpHandler.WhenPost("*")
            .RespondWith((req) =>
            {
                callCount++;
                return (callCount < 3) 
                    ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                    : new HttpResponseMessage(HttpStatusCode.OK);
            });

        var provider = new TestErrorHandlingProvider(HttpClient, LoggerMock.Object);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/post");
        
        var response = await provider.TestFetchWithRetryAsync("https://api.test.com/post", request);
        
        response.IsSuccessStatusCode.ShouldBeTrue();
        callCount.ShouldBe(3);
    }

    [Fact]
    public async Task FetchWithRetryAsync_OnInternalServerError_ShouldRetry()
    {
        var callCount = 0;
        HttpHandler.WhenPost("*")
            .RespondWith((req) =>
            {
                callCount++;
                return (callCount < 2) 
                    ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    : new HttpResponseMessage(HttpStatusCode.OK);
            });

        var provider = new TestErrorHandlingProvider(HttpClient, LoggerMock.Object);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/post");
        
        var response = await provider.TestFetchWithRetryAsync("https://api.test.com/post", request);
        
        response.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact]
    public async Task FetchWithRetryAsync_OnRateLimitExceeded_ShouldRetry()
    {
        var callCount = 0;
        HttpHandler.WhenPost("*")
            .RespondWith((req) =>
            {
                callCount++;
                return (callCount < 2) 
                    ? new HttpResponseMessage(HttpStatusCode.OK) 
                    { 
                        Content = new StringContent("{\"error\": \"rate_limit_exceeded\"}") 
                    }
                    : new HttpResponseMessage(HttpStatusCode.OK) 
                    { 
                        Content = new StringContent("{\"id\": \"123\"}") 
                    };
            });

        var provider = new TestErrorHandlingProvider(HttpClient, LoggerMock.Object);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/post");
        
        var response = await provider.TestFetchWithRetryAsync("https://api.test.com/post", request);
        
        response.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact]
    public async Task FetchWithRetryAsync_OnUnauthorized_ShouldThrowRefreshTokenException()
    {
        HttpHandler.WhenPost("*")
            .RespondWith("{\"error\": \"invalid_token\"}", HttpStatusCode.Unauthorized);

        var provider = new TestErrorHandlingProvider(HttpClient, LoggerMock.Object);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/post");
        
        await Should.ThrowAsync<RefreshTokenException>(() => 
            provider.TestFetchWithRetryAsync("https://api.test.com/post", request));
    }

    [Fact]
    public async Task FetchWithRetryAsync_OnMaxRetriesExceeded_ShouldThrowBadBodyException()
    {
        HttpHandler.WhenPost("*")
            .RespondWith("{\"error\": \"server_error\"}", HttpStatusCode.InternalServerError);

        var provider = new TestErrorHandlingProvider(HttpClient, LoggerMock.Object);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/post");
        
        await Should.ThrowAsync<BadBodyException>(() => 
            provider.TestFetchWithRetryAsync("https://api.test.com/post", request, maxRetries: 1));
    }

    [Fact]
    public async Task FetchWithRetryAsync_WithCustomErrorHandler_ShouldReturnCorrectErrorType()
    {
        HttpHandler.WhenPost("*")
            .RespondWith("{\"error\": \"Error validating access token\"}", HttpStatusCode.BadRequest);

        var provider = new TestErrorHandlingProvider(HttpClient, LoggerMock.Object);
        provider.SetErrorHandlingType(SocialProviderBase.ErrorHandlingType.RefreshToken);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/post");
        
        await Should.ThrowAsync<RefreshTokenException>(() => 
            provider.TestFetchWithRetryAsync("https://api.test.com/post", request));
    }
}

public class TestErrorHandlingProvider : SocialProviderBase
{
    private SocialProviderBase.ErrorHandlingType? _errorHandlingType;
    private string _errorHandlingValue = "";

    public TestErrorHandlingProvider(HttpClient httpClient, ILogger logger) 
        : base(httpClient, logger)
    {
    }

    public void SetErrorHandlingType(SocialProviderBase.ErrorHandlingType type, string value = "")
    {
        _errorHandlingType = type;
        _errorHandlingValue = value;
    }

    protected override ErrorHandlingResult? HandleErrors(string responseBody)
    {
        if (_errorHandlingType.HasValue)
        {
            return new ErrorHandlingResult(_errorHandlingType.Value, _errorHandlingValue);
        }
        return base.HandleErrors(responseBody);
    }

    public async Task<HttpResponseMessage> TestFetchWithRetryAsync(
        string url, 
        HttpRequestMessage request, 
        string identifier = "", 
        int maxRetries = 3)
    {
        return await FetchWithRetryAsync(url, request, identifier, maxRetries);
    }

    public override string Identifier => "test-error";
    public override string Name => "Test Error Provider";
    public override string[] Scopes => Array.Empty<string>();
    public override int MaxLength(object? additionalSettings = null) => 1000;

    public override Task<PostResponse[]> PostAsync(
        string id, string accessToken, List<PostDetails> posts, 
        Integration integration, CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<PostResponse>());

    public override Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new GenerateAuthUrlResponse());

    public override Task<AuthTokenDetails> AuthenticateAsync(
        AuthenticateParams parameters,
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new AuthTokenDetails());

    public override Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new AuthTokenDetails());
}
