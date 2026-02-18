using Aero.Social.Twitter.Client.Correlation;
using Aero.Social.Twitter.Client.Logging;

namespace Aero.Social.Twitter.Correlation;

public class CorrelationIdProviderTests
{
    [Fact]
    public void GuidCorrelationIdProvider_ShouldGenerateUniqueIds()
    {
        // Arrange
        var provider = new GuidCorrelationIdProvider();

        // Act
        var id1 = provider.GenerateCorrelationId();
        var id2 = provider.GenerateCorrelationId();

        // Assert
        Assert.NotEqual(id1, id2);
        Assert.Equal(16, id1.Length); // Should be 16 chars
    }

    [Fact]
    public void GuidCorrelationIdProvider_ShouldGenerateValidIds()
    {
        // Arrange
        var provider = new GuidCorrelationIdProvider();

        // Act
        var id = provider.GenerateCorrelationId();

        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        Assert.DoesNotContain("-", id); // Should not contain hyphens
    }
}

public class CorrelationIdHandlerTests
{
    [Fact]
    public async Task SendAsync_ShouldAddCorrelationIdHeader()
    {
        // Arrange
        var provider = new GuidCorrelationIdProvider();
        var handler = new CorrelationIdHandler(provider)
        {
            InnerHandler = new TestHandler()
        };

        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitter.com/test");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.True(request.Headers.Contains("X-Correlation-Id"));
        var correlationId = request.Headers.GetValues("X-Correlation-Id").FirstOrDefault();
        Assert.NotNull(correlationId);
        Assert.NotEmpty(correlationId);
    }

    [Fact]
    public async Task SendAsync_ShouldNotOverwriteExistingCorrelationId()
    {
        // Arrange
        var provider = new GuidCorrelationIdProvider();
        var handler = new CorrelationIdHandler(provider)
        {
            InnerHandler = new TestHandler()
        };

        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitter.com/test");
        request.Headers.Add("X-Correlation-Id", "existing-id-123");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        var correlationId = request.Headers.GetValues("X-Correlation-Id").FirstOrDefault();
        Assert.Equal("existing-id-123", correlationId);
    }

    private class TestHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}