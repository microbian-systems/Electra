using Aero.Social.Twitter.Client.Clients;
using Aero.Social.Twitter.Client.Exceptions;
using Aero.Social.Twitter.Client.Models;
using NSubstitute;

namespace Aero.Social.Twitter.Clients;

public class TwitterClientPaginationTests
{
    private readonly ITwitterClient _mockClient;

    public TwitterClientPaginationTests()
    {
        _mockClient = Substitute.For<ITwitterClient>();
    }

    [Fact]
    public async Task SearchTweetsPaginatedAsync_WithNullClient_ThrowsArgumentNullException()
    {
        // Arrange
        ITwitterClient? client = null;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            var tweets = client!.SearchTweetsPaginatedAsync("test");
            await foreach (var _ in tweets) { }
        });

        Assert.Equal("client", exception.ParamName);
    }

    [Fact]
    public async Task SearchTweetsPaginatedAsync_WithSinglePage_YieldsAllTweets()
    {
        // Arrange
        var tweets = new List<Tweet>
        {
            new Tweet { Id = "1", Text = "Tweet 1" },
            new Tweet { Id = "2", Text = "Tweet 2" }
        };

        var response = new TweetResponse
        {
            Data = tweets,
            Meta = new TweetMeta
            {
                ResultCount = tweets.Count,
                NextToken = null
            }
        };

        _mockClient
            .SearchTweetsAsync("test", Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = new List<Tweet>();
        await foreach (var tweet in _mockClient.SearchTweetsPaginatedAsync("test"))
        {
            result.Add(tweet);
        }

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("1", result[0].Id);
        Assert.Equal("2", result[1].Id);
        await _mockClient.Received(1).SearchTweetsAsync("test", Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchTweetsPaginatedAsync_WithMultiplePages_YieldsAllTweets()
    {
        // Arrange
        var page1Tweets = new List<Tweet>
        {
            new Tweet { Id = "1", Text = "Tweet 1" },
            new Tweet { Id = "2", Text = "Tweet 2" }
        };

        var page2Tweets = new List<Tweet>
        {
            new Tweet { Id = "3", Text = "Tweet 3" }
        };

        _mockClient
            .SearchTweetsAsync("test", Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>())
            .Returns(
                new TweetResponse
                {
                    Data = page1Tweets,
                    Meta = new TweetMeta { ResultCount = 2, NextToken = "token123" }
                },
                new TweetResponse
                {
                    Data = page2Tweets,
                    Meta = new TweetMeta { ResultCount = 1, NextToken = null }
                }
            );

        // Act
        var result = new List<Tweet>();
        await foreach (var tweet in _mockClient.SearchTweetsPaginatedAsync("test"))
        {
            result.Add(tweet);
        }

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("1", result[0].Id);
        Assert.Equal("2", result[1].Id);
        Assert.Equal("3", result[2].Id);
        await _mockClient.Received(2).SearchTweetsAsync("test", Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchTweetsPaginatedAsync_WithRateLimitWithoutRetryAfter_PropagatesException()
    {
        // Arrange - rate limit without retry-after should throw
        _mockClient
            .SearchTweetsAsync("test", Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TweetResponse>(
                new TwitterRateLimitException("Rate limited without retry info")));

        // Act & Assert
        await Assert.ThrowsAsync<TwitterRateLimitException>(async () =>
        {
            await foreach (var _ in _mockClient.SearchTweetsPaginatedAsync("test")) { }
        });
    }

    [Fact]
    public async Task SearchTweetsPaginatedAsync_WithRateLimitNoRetryAfter_ThrowsException()
    {
        // Arrange
        _mockClient
            .SearchTweetsAsync("test", Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TweetResponse>(new TwitterRateLimitException("Rate limited without retry-after")));

        // Act & Assert
        await Assert.ThrowsAsync<TwitterRateLimitException>(async () =>
        {
            var tweets = _mockClient.SearchTweetsPaginatedAsync("test");
            await foreach (var _ in tweets) { }
        });
    }

    [Fact]
    public async Task SearchTweetsPaginatedAsync_WithEmptyResponse_DoesNotYield()
    {
        // Arrange
        _mockClient
            .SearchTweetsAsync("test", Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>())
            .Returns(new TweetResponse
            {
                Data = null,
                Meta = new TweetMeta { ResultCount = 0, NextToken = null }
            });

        // Act
        var result = new List<Tweet>();
        await foreach (var tweet in _mockClient.SearchTweetsPaginatedAsync("test"))
        {
            result.Add(tweet);
        }

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchTweetsPaginatedAsync_WithOptions_PreservesOptions()
    {
        // Arrange
        var options = new SearchOptions
        {
            MaxResults = 50,
            SinceId = "100",
            UntilId = "200",
            TweetFields = TweetFields.AuthorId | TweetFields.CreatedAt,
            Expansions = ExpansionOptions.AuthorId
        };

        _mockClient
            .SearchTweetsAsync("test", Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>())
            .Returns(new TweetResponse
            {
                Data = new List<Tweet>(),
                Meta = new TweetMeta { ResultCount = 0, NextToken = null }
            });

        // Act
        await foreach (var _ in _mockClient.SearchTweetsPaginatedAsync("test", options)) { }

        // Assert
        await _mockClient.Received(1).SearchTweetsAsync(
            "test",
            Arg.Is<SearchOptions>(o =>
                o.MaxResults == 50 &&
                o.SinceId == "100" &&
                o.UntilId == "200" &&
                o.TweetFields == (TweetFields.AuthorId | TweetFields.CreatedAt) &&
                o.Expansions == ExpansionOptions.AuthorId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchTweetsPaginatedAsync_WithCancellation_StopsEnumeration()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var callCount = 0;

        _mockClient
            .SearchTweetsAsync("test", Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                callCount++;
                // Cancel after first page to test mid-enumeration cancellation
                if (callCount == 1)
                {
                    cts.Cancel();
                }
                return Task.FromResult(new TweetResponse
                {
                    Data = new List<Tweet> { new Tweet { Id = callCount.ToString(), Text = "Tweet" } },
                    Meta = new TweetMeta { ResultCount = 1, NextToken = $"token{callCount}" }
                });
            });

        // Act
        var result = new List<Tweet>();
        await foreach (var tweet in _mockClient.SearchTweetsPaginatedAsync("test", null, cts.Token))
        {
            result.Add(tweet);
        }

        // Assert - should get first page but not second
        Assert.Single(result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task SearchTweetsPaginatedAsync_PassesCancellationTokenToEachPageRequest()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var passedTokens = new List<CancellationToken>();

        _mockClient
            .SearchTweetsAsync("test", Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                passedTokens.Add(x.Arg<CancellationToken>());
                return Task.FromResult(new TweetResponse
                {
                    Data = new List<Tweet>(),
                    Meta = new TweetMeta { ResultCount = 0, NextToken = null }
                });
            });

        // Act
        await foreach (var _ in _mockClient.SearchTweetsPaginatedAsync("test", null, cts.Token)) { }

        // Assert
        Assert.Single(passedTokens);
        Assert.Equal(cts.Token, passedTokens[0]);
    }

    [Fact]
    public async Task GetUserTweetsPaginatedAsync_WithNullClient_ThrowsArgumentNullException()
    {
        // Arrange
        ITwitterClient? client = null;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            var tweets = client!.GetUserTweetsPaginatedAsync("123456");
            await foreach (var _ in tweets) { }
        });

        Assert.Equal("client", exception.ParamName);
    }

    [Fact]
    public async Task GetUserTweetsPaginatedAsync_WithMultiplePages_YieldsAllTweets()
    {
        // Arrange
        _mockClient
            .GetUserTweetsAsync("123456", Arg.Any<TimelineOptions>(), Arg.Any<CancellationToken>())
            .Returns(
                new TweetResponse
                {
                    Data = new List<Tweet> { new Tweet { Id = "1", Text = "Tweet 1" } },
                    Meta = new TweetMeta { ResultCount = 1, NextToken = "token1" }
                },
                new TweetResponse
                {
                    Data = new List<Tweet> { new Tweet { Id = "2", Text = "Tweet 2" } },
                    Meta = new TweetMeta { ResultCount = 1, NextToken = null }
                }
            );

        // Act
        var result = new List<Tweet>();
        await foreach (var tweet in _mockClient.GetUserTweetsPaginatedAsync("123456"))
        {
            result.Add(tweet);
        }

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetUserMentionsPaginatedAsync_WithMultiplePages_YieldsAllTweets()
    {
        // Arrange
        _mockClient
            .GetUserMentionsAsync("123456", Arg.Any<TimelineOptions>(), Arg.Any<CancellationToken>())
            .Returns(
                new TweetResponse
                {
                    Data = new List<Tweet> { new Tweet { Id = "1", Text = "Mention 1" } },
                    Meta = new TweetMeta { ResultCount = 1, NextToken = "token1" }
                },
                new TweetResponse
                {
                    Data = new List<Tweet> { new Tweet { Id = "2", Text = "Mention 2" } },
                    Meta = new TweetMeta { ResultCount = 1, NextToken = null }
                }
            );

        // Act
        var result = new List<Tweet>();
        await foreach (var tweet in _mockClient.GetUserMentionsPaginatedAsync("123456"))
        {
            result.Add(tweet);
        }

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SearchTweetsPaginatedAsync_HandlesNullDataInResponse()
    {
        // Arrange
        _mockClient
            .SearchTweetsAsync("test", Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>())
            .Returns(new TweetResponse
            {
                Data = null,
                Meta = new TweetMeta { ResultCount = 0, NextToken = null }
            });

        // Act
        var result = new List<Tweet>();
        await foreach (var tweet in _mockClient.SearchTweetsPaginatedAsync("test"))
        {
            result.Add(tweet);
        }

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserTweetsPaginatedAsync_HandlesNullMeta()
    {
        // Arrange
        _mockClient
            .GetUserTweetsAsync("123456", Arg.Any<TimelineOptions>(), Arg.Any<CancellationToken>())
            .Returns(new TweetResponse
            {
                Data = new List<Tweet> { new Tweet { Id = "1", Text = "Tweet 1" } },
                Meta = null
            });

        // Act
        var result = await _mockClient.GetUserTweetsPaginatedAsync("123456").ToListAsync();

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetUserTweetsPaginatedAsync_WithRateLimit_PropagatesException()
    {
        // Arrange - rate limit without retry-after should throw
        _mockClient
            .GetUserTweetsAsync("123456", Arg.Any<TimelineOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TweetResponse>(
                new TwitterRateLimitException("Rate limited without retry info")));

        // Act & Assert
        await Assert.ThrowsAsync<TwitterRateLimitException>(async () =>
        {
            await foreach (var _ in _mockClient.GetUserTweetsPaginatedAsync("123456")) { }
        });
    }

    [Fact]
    public async Task GetUserMentionsPaginatedAsync_WithRateLimit_PropagatesException()
    {
        // Arrange - rate limit without retry-after should throw
        _mockClient
            .GetUserMentionsAsync("123456", Arg.Any<TimelineOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TweetResponse>(
                new TwitterRateLimitException("Rate limited without retry info")));

        // Act & Assert
        await Assert.ThrowsAsync<TwitterRateLimitException>(async () =>
        {
            await foreach (var _ in _mockClient.GetUserMentionsPaginatedAsync("123456")) { }
        });
    }
}