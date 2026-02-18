using Aero.Social.Twitter.Client.Models;
using Bogus;

namespace Aero.Social.Twitter.Integration.Builders;

/// <summary>
/// Builder for creating realistic Twitter API tweet responses using Bogus.
/// </summary>
public class TweetResponseBuilder
{
    private readonly Faker _faker;
    private string? _id;
    private string? _text;
    private DateTimeOffset? _createdAt;
    private string? _authorId;
    private PublicMetrics? _publicMetrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="TweetResponseBuilder"/> class.
    /// </summary>
    public TweetResponseBuilder()
    {
        _faker = new Faker();
    }

    /// <summary>
    /// Sets a specific tweet ID.
    /// </summary>
    public TweetResponseBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets a specific tweet text.
    /// </summary>
    public TweetResponseBuilder WithText(string text)
    {
        _text = text;
        return this;
    }

    /// <summary>
    /// Sets a specific creation date.
    /// </summary>
    public TweetResponseBuilder WithCreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    /// <summary>
    /// Sets a specific author ID.
    /// </summary>
    public TweetResponseBuilder WithAuthorId(string authorId)
    {
        _authorId = authorId;
        return this;
    }

    /// <summary>
    /// Sets specific public metrics.
    /// </summary>
    public TweetResponseBuilder WithPublicMetrics(PublicMetrics metrics)
    {
        _publicMetrics = metrics;
        return this;
    }

    /// <summary>
    /// Generates random metrics using Bogus.
    /// </summary>
    public TweetResponseBuilder WithRandomMetrics()
    {
        _publicMetrics = new PublicMetrics
        {
            RetweetCount = _faker.Random.Int(0, 10000),
            ReplyCount = _faker.Random.Int(0, 5000),
            LikeCount = _faker.Random.Int(0, 50000),
            QuoteCount = _faker.Random.Int(0, 1000)
        };
        return this;
    }

    /// <summary>
    /// Builds a Tweet object with realistic data.
    /// </summary>
    public Tweet Build()
    {
        return new Tweet
        {
            Id = _id ?? GenerateTweetId(),
            Text = _text ?? GenerateTweetText(),
            CreatedAt = _createdAt ?? _faker.Date.RecentOffset(30),
            AuthorId = _authorId ?? GenerateUserId(),
            PublicMetrics = _publicMetrics ?? GenerateRandomMetrics()
        };
    }

    /// <summary>
    /// Builds the Twitter API v2 JSON response structure.
    /// </summary>
    public object BuildApiResponse()
    {
        var tweet = Build();
        return new
        {
            data = new
            {
                id = tweet.Id,
                text = tweet.Text,
                created_at = tweet.CreatedAt.ToString("O"),
                author_id = tweet.AuthorId,
                public_metrics = tweet.PublicMetrics != null ? new
                {
                    retweet_count = tweet.PublicMetrics.RetweetCount,
                    reply_count = tweet.PublicMetrics.ReplyCount,
                    like_count = tweet.PublicMetrics.LikeCount,
                    quote_count = tweet.PublicMetrics.QuoteCount
                } : null
            }
        };
    }

    /// <summary>
    /// Builds the JSON response as a string.
    /// </summary>
    public string BuildJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(BuildApiResponse());
    }

    private static string GenerateTweetId()
    {
        // Twitter IDs are numeric strings (64-bit range)
        var random = new Random();
        return random.NextInt64(1000000000000000000, long.MaxValue).ToString();
    }

    private static string GenerateUserId()
    {
        // Twitter IDs are numeric strings (64-bit range)
        var random = new Random();
        return random.NextInt64(1000000000000000000, long.MaxValue).ToString();
    }

    private string GenerateTweetText()
    {
        var templates = new[]
        {
            "Just deployed a new feature! 🚀 #dotnet #twitter",
            "Working on some exciting updates today 💻",
            "Thanks for all the feedback on the latest release! 🙏",
            "Check out this awesome thread about API design 👇",
            "Coffee and code - perfect morning ☕💻",
            "Anyone else excited about .NET 10? 🎉",
            "Learning something new every day 📚",
            "Building things that matter 💪"
        };

        return _faker.PickRandom(templates);
    }

    private PublicMetrics GenerateRandomMetrics()
    {
        return new PublicMetrics
        {
            RetweetCount = _faker.Random.Int(0, 1000),
            ReplyCount = _faker.Random.Int(0, 500),
            LikeCount = _faker.Random.Int(0, 5000),
            QuoteCount = _faker.Random.Int(0, 100)
        };
    }
}