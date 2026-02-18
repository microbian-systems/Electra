using System.Net;
using Aero.Social.Twitter.Client.Clients;
using Aero.Social.Twitter.Client.Configuration;
using Aero.Social.Twitter.Client.Exceptions;
using Aero.Social.Twitter.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Aero.Social.Twitter.Clients;

public class TwitterClientTimelineTests
{
    private readonly IOptions<TwitterClientOptions> _options;
    private readonly ILogger<TwitterClient> _logger;

    public TwitterClientTimelineTests()
    {
        _options = Options.Create(new TwitterClientOptions
        {
            BearerToken = "test_bearer_token"
        });
        _logger = Substitute.For<ILogger<TwitterClient>>();
    }

    #region GetUserTweetsAsync Tests

    [Fact]
    public async Task GetUserTweetsAsync_WithValidUserId_ReturnsTweets()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(request =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{
                        ""data"": [
                            {
                                ""id"": ""1234567890"",
                                ""text"": ""My first tweet"",
                                ""created_at"": ""2020-01-01T00:00:00.000Z"",
                                ""author_id"": ""9876543210""
                            },
                            {
                                ""id"": ""1234567891"",
                                ""text"": ""My second tweet"",
                                ""created_at"": ""2020-01-02T00:00:00.000Z"",
                                ""author_id"": ""9876543210""
                            }
                        ],
                        ""meta"": {
                            ""result_count"": 2,
                            ""next_token"": ""next_page_token"",
                            ""newest_id"": ""1234567891"",
                            ""oldest_id"": ""1234567890""
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
        var result = await twitterClient.GetUserTweetsAsync("9876543210");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal("1234567890", result.Data[0].Id);
        Assert.Equal("1234567891", result.Data[1].Id);
        Assert.NotNull(result.Meta);
        Assert.Equal("next_page_token", result.Meta.NextToken);
    }

    [Fact]
    public async Task GetUserTweetsAsync_WithOptions_IncludesAllParameters()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHttpMessageHandler(request =>
        {
            capturedRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{
                        ""data"": [],
                        ""meta"": {
                            ""result_count"": 0
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

        var options = new TimelineOptions
        {
            MaxResults = 50,
            SinceId = "1000000000",
            UntilId = "9999999999",
            StartTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero),
            PaginationToken = "b26v89c19zqg8o3f",
            Exclude = "retweets,replies",
            TweetFields = TweetFields.PublicMetrics | TweetFields.CreatedAt,
            Expansions = ExpansionOptions.AuthorId
        };

        // Act
        await twitterClient.GetUserTweetsAsync("9876543210", options);

        // Assert
        Assert.NotNull(capturedRequest);
        var query = capturedRequest.RequestUri?.Query;
        Assert.Contains("max_results=50", query);
        Assert.Contains("since_id=1000000000", query);
        Assert.Contains("until_id=9999999999", query);
        Assert.Contains("start_time=", query);
        Assert.Contains("end_time=", query);
        Assert.Contains("pagination_token=b26v89c19zqg8o3f", query);
        Assert.Contains("exclude=retweets%2Creplies", query);
        Assert.Contains("tweet.fields=", query);
        Assert.Contains("expansions=author_id", query);
    }

    [Fact]
    public async Task GetUserTweetsAsync_WithNullUserId_ThrowsArgumentException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var twitterClient = new TwitterClient(httpClient, _options, _logger);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            twitterClient.GetUserTweetsAsync(null!));
        Assert.Contains("User ID cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task GetUserTweetsAsync_WithInvalidMaxResults_ThrowsArgumentException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var twitterClient = new TwitterClient(httpClient, _options, _logger);

        var options = new TimelineOptions
        {
            MaxResults = 3 // Invalid: must be >= 5
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            twitterClient.GetUserTweetsAsync("9876543210", options));
        Assert.Contains("MaxResults must be between 5 and 100", exception.Message);
    }

    [Fact]
    public async Task GetUserTweetsAsync_WithNotFound_ThrowsTwitterApiException()
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
            twitterClient.GetUserTweetsAsync("nonexistent"));
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    #endregion

    #region GetUserMentionsAsync Tests

    [Fact]
    public async Task GetUserMentionsAsync_WithValidUserId_ReturnsMentions()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(request =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{
                        ""data"": [
                            {
                                ""id"": ""1234567890"",
                                ""text"": ""@testuser Hello!"",
                                ""created_at"": ""2020-01-01T00:00:00.000Z"",
                                ""author_id"": ""1111111111""
                            },
                            {
                                ""id"": ""1234567891"",
                                ""text"": ""@testuser How are you?"",
                                ""created_at"": ""2020-01-02T00:00:00.000Z"",
                                ""author_id"": ""2222222222""
                            }
                        ],
                        ""meta"": {
                            ""result_count"": 2,
                            ""newest_id"": ""1234567891"",
                            ""oldest_id"": ""1234567890""
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
        var result = await twitterClient.GetUserMentionsAsync("9876543210");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains("@testuser", result.Data[0].Text);
        Assert.Contains("@testuser", result.Data[1].Text);
    }

    [Fact]
    public async Task GetUserMentionsAsync_WithOptions_IncludesParameters()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHttpMessageHandler(request =>
        {
            capturedRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{
                        ""data"": [],
                        ""meta"": {
                            ""result_count"": 0
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

        var options = new TimelineOptions
        {
            MaxResults = 25,
            PaginationToken = "test_token",
            TweetFields = TweetFields.PublicMetrics
        };

        // Act
        await twitterClient.GetUserMentionsAsync("9876543210", options);

        // Assert
        Assert.NotNull(capturedRequest);
        var query = capturedRequest.RequestUri?.Query;
        Assert.Contains("max_results=25", query);
        Assert.Contains("pagination_token=test_token", query);
        Assert.Contains("tweet.fields=public_metrics", query);
    }

    [Fact]
    public async Task GetUserMentionsAsync_WithNullUserId_ThrowsArgumentException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var twitterClient = new TwitterClient(httpClient, _options, _logger);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            twitterClient.GetUserMentionsAsync(null!));
        Assert.Contains("User ID cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task GetUserMentionsAsync_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(request =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{
                        ""data"": [],
                        ""meta"": {
                            ""result_count"": 0
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
        var result = await twitterClient.GetUserMentionsAsync("9876543210");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.Meta?.ResultCount);
    }

    #endregion

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