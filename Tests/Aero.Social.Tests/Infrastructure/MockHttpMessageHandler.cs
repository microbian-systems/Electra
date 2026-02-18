using System.Net;
using System.Text;
using System.Text.Json;

namespace Aero.Social.Tests.Infrastructure;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly List<MockedRequest> _mockedRequests = new();
    private readonly List<HttpRequestMessage> _receivedRequests = new();

    public IReadOnlyList<HttpRequestMessage> ReceivedRequests => _receivedRequests;

    public MockHttpMessageHandler When(Func<HttpRequestMessage, bool> predicate)
    {
        _mockedRequests.Add(new MockedRequest { Predicate = predicate });
        return this;
    }

    public MockHttpMessageHandler WhenGet(string urlPattern)
    {
        return When(req => req.Method == HttpMethod.Get && MatchUrl(req, urlPattern));
    }

    public MockHttpMessageHandler WhenPost(string urlPattern)
    {
        return When(req => req.Method == HttpMethod.Post && MatchUrl(req, urlPattern));
    }

    public MockedRequest RespondWith(string content, HttpStatusCode statusCode = HttpStatusCode.OK, string contentType = "application/json")
    {
        var mock = _mockedRequests.Last();
        mock.ResponseContent = content;
        mock.StatusCode = statusCode;
        mock.ContentType = contentType;
        return mock;
    }

    public MockedRequest RespondWith(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var mock = _mockedRequests.Last();
        mock.ResponseFactory = responseFactory;
        return mock;
    }

    public MockedRequest RespondWithJson<T>(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return RespondWith(json, statusCode);
    }

    public MockedRequest RespondWithStatusCode(HttpStatusCode statusCode)
    {
        var mock = _mockedRequests.Last();
        mock.StatusCode = statusCode;
        return mock;
    }

    private static bool MatchUrl(HttpRequestMessage request, string pattern)
    {
        if (request.RequestUri == null) return false;
        var url = request.RequestUri.ToString();
        if (pattern.Contains('*'))
        {
            var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(url, regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return url.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _receivedRequests.Add(request);

        foreach (var mock in _mockedRequests)
        {
            if (mock.Predicate(request))
            {
                await Task.Delay(mock.DelayMs, cancellationToken);

                if (mock.ThrowException != null)
                {
                    throw mock.ThrowException;
                }

                if (mock.ResponseFactory != null)
                {
                    return mock.ResponseFactory(request);
                }

                var response = new HttpResponseMessage(mock.StatusCode)
                {
                    Content = mock.ResponseContent != null
                        ? new StringContent(mock.ResponseContent, Encoding.UTF8, mock.ContentType)
                        : null
                };

                return response;
            }
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent($"{{\"error\": \"No mock configured for {request.Method} {request.RequestUri}\"}}")
        };
    }

    public void Reset()
    {
        _mockedRequests.Clear();
        _receivedRequests.Clear();
    }
}

public class MockedRequest
{
    public Func<HttpRequestMessage, bool> Predicate { get; set; } = _ => false;
    public string? ResponseContent { get; set; }
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public string ContentType { get; set; } = "application/json";
    public int DelayMs { get; set; }
    public Exception? ThrowException { get; set; }
    public Func<HttpRequestMessage, HttpResponseMessage>? ResponseFactory { get; set; }

    public MockedRequest WithDelay(int milliseconds)
    {
        DelayMs = milliseconds;
        return this;
    }

    public MockedRequest Throw(Exception exception)
    {
        ThrowException = exception;
        return this;
    }
}
