using System.Net;
using Aero.Social.Twitter.Client.Exceptions;
using Aero.Social.Twitter.Client.Models;
using Aero.Social.Twitter.Integration.Builders;
using AutoFixture;
using AutoFixture.Xunit2;
using Bogus;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Aero.Social.Twitter.Integration;

[Trait("Category", "Integration")]
[Collection("Integration")]
public class TwitterApiIntegrationTests : IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly Faker _faker;
    private readonly Fixture _autoFixture;
    private bool _disposed;

    public TwitterApiIntegrationTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _faker = new Faker();
        _autoFixture = new Fixture();
        _fixture.Reset();
    }

    #region Happy Path Tests

    [Fact]
    public async Task GetTweetAsync_WithValidId_ReturnsTweet()
    {
        // Arrange - Using Bogus for realistic data
        var tweetId = _faker.Random.Long(1000000000000000000, long.MaxValue).ToString();

        _fixture.Server
            .Given(
                Request.Create()
                    .WithPath($"/2/tweets/{tweetId}")
                    .UsingGet()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(new TweetResponseBuilder()
                        .WithId(tweetId)
                        .WithText("Test tweet content")
                        .BuildJson())
            );

        var client = _fixture.CreateClient();

        // Act
        var result = await client.GetTweetAsync(tweetId);

        // Assert - Using Shouldly
        result.ShouldNotBeNull();
        result.Id.ShouldBe(tweetId);
        result.Text.ShouldBe("Test tweet content");
    }

    [Theory]
    [AutoData]
    public async Task GetTweetAsync_WithAutoFixtureData_ReturnsTweet(string tweetId, string tweetText)
    {
        // Arrange - Using AutoFixture
        _fixture.Server
            .Given(
                Request.Create()
                    .WithPath($"/2/tweets/{tweetId}")
                    .UsingGet()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(new TweetResponseBuilder()
                        .WithId(tweetId)
                        .WithText(tweetText)
                        .BuildJson())
            );

        var client = _fixture.CreateClient();

        // Act
        var result = await client.GetTweetAsync(tweetId);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(tweetId);
        result.Text.ShouldBe(tweetText);
    }

    [Fact]
    public async Task GetTweetAsync_WithOAuth2_SendsBearerToken()
    {
        // Arrange
        var tweetId = "1234567890";

        _fixture.Server
            .Given(
                Request.Create()
                    .WithPath($"/2/tweets/{tweetId}")
                    .UsingGet()
                    .WithHeader("Authorization", "Bearer my_test_bearer_token_12345", true)
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBody(new TweetResponseBuilder().WithId(tweetId).BuildJson())
            );

        var client = _fixture.CreateClient(opts =>
        {
            opts.BearerToken = "my_test_bearer_token_12345";
        });

        // Act
        var result = await client.GetTweetAsync(tweetId);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(tweetId);
    }

    [Fact]
    public async Task GetTweetAsync_DeserializesPublicMetrics()
    {
        // Arrange
        var tweetId = "1234567890";
        var expectedMetrics = new PublicMetrics
        {
            RetweetCount = 42,
            ReplyCount = 7,
            LikeCount = 156,
            QuoteCount = 12
        };

        _fixture.Server
            .Given(
                Request.Create()
                    .WithPath($"/2/tweets/{tweetId}")
                    .UsingGet()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBody(new TweetResponseBuilder()
                        .WithId(tweetId)
                        .WithPublicMetrics(expectedMetrics)
                        .BuildJson())
            );

        var client = _fixture.CreateClient();

        // Act
        var result = await client.GetTweetAsync(tweetId);

        // Assert
        result.PublicMetrics.ShouldNotBeNull();
        result.PublicMetrics.RetweetCount.ShouldBe(42);
        result.PublicMetrics.ReplyCount.ShouldBe(7);
        result.PublicMetrics.LikeCount.ShouldBe(156);
        result.PublicMetrics.QuoteCount.ShouldBe(12);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetTweetAsync_WithInvalidOAuth1_ThrowsAuthenticationException()
    {
        // Arrange
        var tweetId = "1234567890";

        _fixture.Server
            .Given(
                Request.Create()
                    .WithPath($"/2/tweets/{tweetId}")
                    .UsingGet()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(401)
                    .WithBody(ErrorResponseBuilder.Unauthorized().BuildJson())
            );

        var client = _fixture.CreateClient();

        // Act & Assert - Using Shouldly
        var exception = await Should.ThrowAsync<TwitterAuthenticationException>(
            async () => await client.GetTweetAsync(tweetId));

        exception.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTweetAsync_WithInvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var tweetId = "invalid_id";

        _fixture.Server
            .Given(
                Request.Create()
                    .WithPath($"/2/tweets/{tweetId}")
                    .UsingGet()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(404)
                    .WithBody(ErrorResponseBuilder.NotFound("Tweet").BuildJson())
            );

        var client = _fixture.CreateClient();

        // Act & Assert
        var exception = await Should.ThrowAsync<TwitterApiException>(
            async () => await client.GetTweetAsync(tweetId));

        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(900)]  // 15 minutes
    [InlineData(60)]   // 1 minute
    public async Task GetTweetAsync_WhenRateLimited_ThrowsRateLimitExceptionWithRetryAfter(int retryAfterSeconds)
    {
        // Arrange
        var tweetId = "1234567890";

        _fixture.Server
            .Given(
                Request.Create()
                    .WithPath($"/2/tweets/{tweetId}")
                    .UsingGet()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(429)
                    .WithHeader("Retry-After", retryAfterSeconds.ToString())
                    .WithBody(ErrorResponseBuilder.RateLimited(retryAfterSeconds).BuildJson())
            );

        var client = _fixture.CreateClient();

        // Act & Assert
        var exception = await Should.ThrowAsync<TwitterRateLimitException>(
            async () => await client.GetTweetAsync(tweetId));

        exception.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        exception.RetryAfter.ShouldNotBeNull();
        exception.RetryAfter.Value.TotalSeconds.ShouldBe(retryAfterSeconds);
    }

    [Fact]
    public async Task GetTweetAsync_WithServerError_ThrowsApiException()
    {
        // Arrange
        var tweetId = "1234567890";

        _fixture.Server
            .Given(
                Request.Create()
                    .WithPath($"/2/tweets/{tweetId}")
                    .UsingGet()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(500)
                    .WithBody(ErrorResponseBuilder.ServerError().BuildJson())
            );

        var client = _fixture.CreateClient();

        // Act & Assert
        var exception = await Should.ThrowAsync<TwitterApiException>(
            async () => await client.GetTweetAsync(tweetId));

        exception.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Request Validation Tests

    [Fact]
    public async Task GetTweetAsync_MakesRequestToCorrectEndpoint()
    {
        // Arrange
        var tweetId = "1234567890";

        _fixture.Server
            .Given(
                Request.Create()
                    .WithPath($"/2/tweets/{tweetId}")
                    .UsingGet()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBody(new TweetResponseBuilder().WithId(tweetId).BuildJson())
            );

        var client = _fixture.CreateClient();

        // Act
        var result = await client.GetTweetAsync(tweetId);

        // Assert
        result.ShouldNotBeNull();
        _fixture.Server.LogEntries.ShouldNotBeEmpty();
        _fixture.Server.LogEntries.ShouldContain(entry => 
            entry.RequestMessage.Path == $"/2/tweets/{tweetId}");
    }

    [Fact]
    public async Task GetTweetAsync_UsesGetMethod()
    {
        // Arrange
        var tweetId = "1234567890";

        _fixture.Server
            .Given(
                Request.Create()
                    .WithPath($"/2/tweets/{tweetId}")
                    .UsingGet()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBody(new TweetResponseBuilder().WithId(tweetId).BuildJson())
            );

        var client = _fixture.CreateClient();

        // Act
        var result = await client.GetTweetAsync(tweetId);

        // Assert
        result.ShouldNotBeNull();
        _fixture.Server.LogEntries.ShouldContain(entry =>
            entry.RequestMessage.Method == "GET");
    }

    #endregion

    #region Retry Behavior Tests

    [Fact]
    public async Task GetTweetAsync_WithTransientFailure_RetriesAndSucceeds()
    {
        // Arrange - Test retry behavior with transient failure
        var tweetId = "1234567890";

        // Setup WireMock to return 503 Service Unavailable (transient failure)
        _fixture.Server
            .Given(
                Request.Create()
                    .WithPath($"/2/tweets/{tweetId}")
                    .UsingGet()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(503)
                    .WithBody(ErrorResponseBuilder.ServiceUnavailable().BuildJson())
            );

        var client = _fixture.CreateClient();

        // Act & Assert - Should throw after retries
        await Should.ThrowAsync<TwitterApiException>(
            async () => await client.GetTweetAsync(tweetId));

        // Verify multiple requests were made (retry occurred)
        _fixture.Server.LogEntries.Count().ShouldBeGreaterThanOrEqualTo(1);
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _fixture.Reset();
            _disposed = true;
        }
    }
}