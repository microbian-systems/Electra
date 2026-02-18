using System.Net;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aero.Social.Twitter.Logging;

public class LoggingHttpMessageHandlerTests
{
    [Fact]
    public async Task SendAsync_WithSuccessfulRequest_ShouldLogRequestAndResponse()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingHttpMessageHandler>>();
        var handler = new LoggingHttpMessageHandler(logger)
        {
            InnerHandler = new TestHandler(HttpStatusCode.OK, "OK")
        };

        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitter.com/test");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("HTTP Request")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("HTTP Response")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task SendAsync_WithErrorResponse_ShouldLogWarning()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingHttpMessageHandler>>();
        var handler = new LoggingHttpMessageHandler(logger)
        {
            InnerHandler = new TestHandler(HttpStatusCode.NotFound, "Not Found")
        };

        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitter.com/test");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("HTTP Response") && o.ToString()!.Contains("404")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task SendAsync_WithException_ShouldLogError()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingHttpMessageHandler>>();
        var expectedException = new HttpRequestException("Connection failed");
        var handler = new LoggingHttpMessageHandler(logger)
        {
            InnerHandler = new ExceptionHandler(expectedException)
        };

        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitter.com/test");

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));

        logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("HTTP request failed")),
            Arg.Any<HttpRequestException>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task SendAsync_ShouldRedactAuthorizationHeader()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingHttpMessageHandler>>();
        logger.IsEnabled(LogLevel.Debug).Returns(true);

        var handler = new LoggingHttpMessageHandler(logger)
        {
            InnerHandler = new TestHandler(HttpStatusCode.OK, "OK")
        };

        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitter.com/test");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "secret_token_123");

        // Act
        await client.SendAsync(request);

        // Assert
        logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Authorization") && o.ToString()!.Contains("[REDACTED]")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    private class TestHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _reasonPhrase;

        public TestHandler(HttpStatusCode statusCode, string reasonPhrase)
        {
            _statusCode = statusCode;
            _reasonPhrase = reasonPhrase;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                ReasonPhrase = _reasonPhrase
            });
        }
    }

    private class ExceptionHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ExceptionHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }
}