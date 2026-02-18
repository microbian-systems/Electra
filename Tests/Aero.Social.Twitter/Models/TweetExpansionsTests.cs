using Aero.Social.Twitter.Client.Models;

namespace Aero.Social.Twitter.Models;

public class TweetExpansionsTests
{
    [Fact]
    public void Tweet_WithAuthorExpansion_ReturnsCorrectAuthor()
    {
        // Arrange
        var user = new User { Id = "123", Username = "testuser", Name = "Test User" };
        var tweet = new Tweet { Id = "1", Text = "Hello", AuthorId = "123" };
        var includes = new Includes { Users = new List<User> { user } };

        // Act
        var author = includes.Users?.FirstOrDefault(u => u.Id == tweet.AuthorId);

        // Assert
        Assert.NotNull(author);
        Assert.Equal("testuser", author.Username);
    }

    [Fact]
    public void Tweet_WithNullIncludes_ReturnsNullAuthor()
    {
        // Arrange
        var tweet = new Tweet { Id = "1", Text = "Hello", AuthorId = "123" };
        Includes? includes = null;

        // Act
        var author = includes?.Users?.FirstOrDefault(u => u.Id == tweet.AuthorId);

        // Assert
        Assert.Null(author);
    }

    [Fact]
    public void Tweet_WithEmptyUsersList_ReturnsNullAuthor()
    {
        // Arrange
        var tweet = new Tweet { Id = "1", Text = "Hello", AuthorId = "123" };
        var includes = new Includes { Users = new List<User>() };

        // Act
        var author = includes.Users?.FirstOrDefault(u => u.Id == tweet.AuthorId);

        // Assert
        Assert.Null(author);
    }

    [Fact]
    public void Tweet_WithReferencedTweetsExpansion_ReturnsReferencedTweets()
    {
        // Arrange
        var referencedTweet = new Tweet { Id = "456", Text = "Original tweet" };
        var tweet = new Tweet { Id = "1", Text = "RT" };
        var includes = new Includes { Tweets = new List<Tweet> { referencedTweet } };

        // Act
        var found = includes.Tweets?.FirstOrDefault(t => t.Id == "456");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Original tweet", found.Text);
    }

    [Fact]
    public void Tweet_WithMediaExpansion_ReturnsMediaObjects()
    {
        // Arrange
        var media = new Media { MediaKey = "media_1", Type = "photo", Url = "https://example.com/image.jpg" };
        var tweet = new Tweet { Id = "1", Text = "Check out this photo" };
        var includes = new Includes { Media = new List<Media> { media } };

        // Act
        var found = includes.Media?.FirstOrDefault(m => m.MediaKey == "media_1");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("photo", found.Type);
    }

    [Fact]
    public void Tweet_WithNullAuthorId_ReturnsNullAuthor()
    {
        // Arrange
        var user = new User { Id = "123", Username = "testuser" };
        var tweet = new Tweet { Id = "1", Text = "Hello", AuthorId = null };
        var includes = new Includes { Users = new List<User> { user } };

        // Act
        var author = includes.Users?.FirstOrDefault(u => u.Id == tweet.AuthorId);

        // Assert
        Assert.Null(author);
    }

    [Fact]
    public void TweetResponse_WithIncludes_ProvidesAccessToExpansions()
    {
        // Arrange
        var user = new User { Id = "123", Username = "testuser", Name = "Test User" };
        var tweet = new Tweet { Id = "1", Text = "Hello", AuthorId = "123" };
        var response = new TweetResponse
        {
            Data = new List<Tweet> { tweet },
            Includes = new Includes { Users = new List<User> { user } }
        };

        // Act
        var author = response.Includes?.Users?.FirstOrDefault(u => u.Id == tweet.AuthorId);

        // Assert
        Assert.NotNull(author);
        Assert.Equal("testuser", author.Username);
    }
}