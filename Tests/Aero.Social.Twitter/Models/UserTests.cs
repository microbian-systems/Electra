using System.Text.Json;
using Aero.Social.Twitter.Client.Models;

namespace Aero.Social.Twitter.Models;

public class UserTests
{
    [Fact]
    public void User_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var user = new User
        {
            Id = "1234567890" // Id is required
        };

        // Assert
        Assert.NotNull(user.Id);
        Assert.Equal(default(DateTimeOffset), user.CreatedAt);
        Assert.False(user.Verified);
    }

    [Fact]
    public void User_Serialization_WithAllProperties_ReturnsCorrectJson()
    {
        // Arrange
        var user = new User
        {
            Id = "1234567890",
            Name = "Test User",
            Username = "testuser",
            CreatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Description = "This is a test user",
            Location = "Test Location",
            ProfileImageUrl = "https://example.com/image.jpg",
            Verified = true,
            Url = "https://example.com",
            VerifiedType = "blue",
            PublicMetrics = new UserPublicMetrics
            {
                FollowersCount = 100,
                FollowingCount = 50,
                TweetCount = 25,
                ListedCount = 10
            }
        };

        // Act
        var json = JsonSerializer.Serialize(user);

        // Assert
        Assert.Contains("\"id\":\"1234567890\"", json);
        Assert.Contains("\"name\":\"Test User\"", json);
        Assert.Contains("\"username\":\"testuser\"", json);
        Assert.Contains("\"created_at\":\"2020-01-01T00:00:00+00:00\"", json);
        Assert.Contains("\"description\":\"This is a test user\"", json);
        Assert.Contains("\"location\":\"Test Location\"", json);
        Assert.Contains("\"profile_image_url\":\"https://example.com/image.jpg\"", json);
        Assert.Contains("\"verified\":true", json);
        Assert.Contains("\"url\":\"https://example.com\"", json);
        Assert.Contains("\"verified_type\":\"blue\"", json);
        Assert.Contains("\"public_metrics\"", json);
    }

    [Fact]
    public void User_Serialization_WithMinimalProperties_ReturnsCorrectJson()
    {
        // Arrange
        var user = new User
        {
            Id = "1234567890",
            Name = "Test User",
            Username = "testuser",
            CreatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        // Act
        var json = JsonSerializer.Serialize(user);

        // Assert
        Assert.Contains("\"id\":\"1234567890\"", json);
        Assert.Contains("\"name\":\"Test User\"", json);
        Assert.Contains("\"username\":\"testuser\"", json);
        Assert.Contains("\"verified\":false", json);
    }

    [Fact]
    public void User_Deserialization_WithAllProperties_PopulatesCorrectly()
    {
        // Arrange
        var json = @"{
                ""id"": ""1234567890"",
                ""name"": ""Test User"",
                ""username"": ""testuser"",
                ""created_at"": ""2020-01-01T00:00:00.000Z"",
                ""description"": ""This is a test user"",
                ""location"": ""Test Location"",
                ""profile_image_url"": ""https://example.com/image.jpg"",
                ""verified"": true,
                ""url"": ""https://example.com"",
                ""verified_type"": ""blue"",
                ""public_metrics"": {
                    ""followers_count"": 100,
                    ""following_count"": 50,
                    ""tweet_count"": 25,
                    ""listed_count"": 10
                }
            }";

        // Act
        var user = JsonSerializer.Deserialize<User>(json);

        // Assert
        Assert.NotNull(user);
        Assert.Equal("1234567890", user.Id);
        Assert.Equal("Test User", user.Name);
        Assert.Equal("testuser", user.Username);
        Assert.Equal(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), user.CreatedAt);
        Assert.Equal("This is a test user", user.Description);
        Assert.Equal("Test Location", user.Location);
        Assert.Equal("https://example.com/image.jpg", user.ProfileImageUrl);
        Assert.True(user.Verified);
        Assert.Equal("https://example.com", user.Url);
        Assert.Equal("blue", user.VerifiedType);
        Assert.NotNull(user.PublicMetrics);
        Assert.Equal(100, user.PublicMetrics.FollowersCount);
    }

    [Fact]
    public void User_Deserialization_WithMinimalProperties_PopulatesCorrectly()
    {
        // Arrange
        var json = @"{
                ""id"": ""1234567890"",
                ""name"": ""Test User"",
                ""username"": ""testuser"",
                ""created_at"": ""2020-01-01T00:00:00.000Z""
            }";

        // Act
        var user = JsonSerializer.Deserialize<User>(json);

        // Assert
        Assert.NotNull(user);
        Assert.Equal("1234567890", user.Id);
        Assert.Equal("Test User", user.Name);
        Assert.Equal("testuser", user.Username);
        Assert.Equal(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), user.CreatedAt);
        Assert.Null(user.Description);
        Assert.Null(user.Location);
        Assert.Null(user.ProfileImageUrl);
        Assert.False(user.Verified);
        Assert.Null(user.Url);
        Assert.Null(user.VerifiedType);
        Assert.Null(user.PublicMetrics);
    }

    [Fact]
    public void User_Deserialization_WithNullFields_HandlesCorrectly()
    {
        // Arrange
        var json = @"{
                ""id"": ""1234567890"",
                ""name"": null,
                ""username"": null,
                ""created_at"": ""2020-01-01T00:00:00.000Z"",
                ""description"": null,
                ""public_metrics"": null
            }";

        // Act
        var user = JsonSerializer.Deserialize<User>(json);

        // Assert
        Assert.NotNull(user);
        Assert.Equal("1234567890", user.Id);
        Assert.Null(user.Name);
        Assert.Null(user.Username);
        Assert.Null(user.Description);
        Assert.Null(user.PublicMetrics);
    }
}