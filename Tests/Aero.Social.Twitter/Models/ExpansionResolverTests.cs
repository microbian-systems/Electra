using Aero.Social.Twitter.Client.Models;

namespace Aero.Social.Twitter.Models;

public class ExpansionResolverTests
{
    #region ResolveAuthor Tests

    [Fact]
    public void ResolveAuthor_WithValidAuthorId_ReturnsUser()
    {
        // Arrange
        var user = new User { Id = "123", Username = "testuser", Name = "Test User" };
        var tweet = new Tweet { Id = "1", Text = "Hello", AuthorId = "123" };
        var includes = new Includes { Users = new List<User> { user } };

        // Act
        var author = tweet.ResolveAuthor(includes);

        // Assert
        Assert.NotNull(author);
        Assert.Equal("testuser", author.Username);
        Assert.Equal("Test User", author.Name);
    }

    [Fact]
    public void ResolveAuthor_WithNullTweet_ReturnsNull()
    {
        // Arrange
        Tweet? tweet = null;
        var includes = new Includes { Users = new List<User>() };

        // Act
        var author = tweet.ResolveAuthor(includes);

        // Assert
        Assert.Null(author);
    }

    [Fact]
    public void ResolveAuthor_WithNullAuthorId_ReturnsNull()
    {
        // Arrange
        var tweet = new Tweet { Id = "1", Text = "Hello", AuthorId = null };
        var includes = new Includes { Users = new List<User> { new User { Id = "123" } } };

        // Act
        var author = tweet.ResolveAuthor(includes);

        // Assert
        Assert.Null(author);
    }

    [Fact]
    public void ResolveAuthor_WithNullIncludes_ReturnsNull()
    {
        // Arrange
        var tweet = new Tweet { Id = "1", Text = "Hello", AuthorId = "123" };
        Includes? includes = null;

        // Act
        var author = tweet.ResolveAuthor(includes);

        // Assert
        Assert.Null(author);
    }

    [Fact]
    public void ResolveAuthor_WithEmptyUsersList_ReturnsNull()
    {
        // Arrange
        var tweet = new Tweet { Id = "1", Text = "Hello", AuthorId = "123" };
        var includes = new Includes { Users = new List<User>() };

        // Act
        var author = tweet.ResolveAuthor(includes);

        // Assert
        Assert.Null(author);
    }

    [Fact]
    public void ResolveAuthor_WithUserNotFound_ReturnsNull()
    {
        // Arrange
        var tweet = new Tweet { Id = "1", Text = "Hello", AuthorId = "123" };
        var includes = new Includes { Users = new List<User> { new User { Id = "456", Username = "other" } } };

        // Act
        var author = tweet.ResolveAuthor(includes);

        // Assert
        Assert.Null(author);
    }

    #endregion

    #region ResolveUser Tests

    [Fact]
    public void ResolveUser_WithValidUserId_ReturnsUser()
    {
        // Arrange
        var includes = new Includes { Users = new List<User> { new User { Id = "123", Username = "testuser" } } };

        // Act
        var user = includes.ResolveUser("123");

        // Assert
        Assert.NotNull(user);
        Assert.Equal("testuser", user.Username);
    }

    [Fact]
    public void ResolveUser_WithNullIncludes_ReturnsNull()
    {
        // Arrange
        Includes? includes = null;

        // Act
        var user = includes.ResolveUser("123");

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public void ResolveUser_WithNullUserId_ReturnsNull()
    {
        // Arrange
        var includes = new Includes { Users = new List<User> { new User { Id = "123" } } };

        // Act
        var user = includes.ResolveUser(null);

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public void ResolveUser_WithEmptyUserId_ReturnsNull()
    {
        // Arrange
        var includes = new Includes { Users = new List<User> { new User { Id = "123" } } };

        // Act
        var user = includes.ResolveUser("");

        // Assert
        Assert.Null(user);
    }

    #endregion

    #region ResolveTweet Tests

    [Fact]
    public void ResolveTweet_WithValidTweetId_ReturnsTweet()
    {
        // Arrange
        var includes = new Includes { Tweets = new List<Tweet> { new Tweet { Id = "456", Text = "Original tweet" } } };

        // Act
        var tweet = includes.ResolveTweet("456");

        // Assert
        Assert.NotNull(tweet);
        Assert.Equal("Original tweet", tweet.Text);
    }

    [Fact]
    public void ResolveTweet_WithNullIncludes_ReturnsNull()
    {
        // Arrange
        Includes? includes = null;

        // Act
        var tweet = includes.ResolveTweet("456");

        // Assert
        Assert.Null(tweet);
    }

    [Fact]
    public void ResolveTweet_WithTweetNotFound_ReturnsNull()
    {
        // Arrange
        var includes = new Includes { Tweets = new List<Tweet> { new Tweet { Id = "789" } } };

        // Act
        var tweet = includes.ResolveTweet("456");

        // Assert
        Assert.Null(tweet);
    }

    #endregion

    #region ResolveMedia (Single) Tests

    [Fact]
    public void ResolveMedia_WithValidMediaKey_ReturnsMedia()
    {
        // Arrange
        var includes = new Includes { Media = new List<Media> { new Media { MediaKey = "media_1", Type = "photo" } } };

        // Act
        var media = includes.ResolveMedia("media_1");

        // Assert
        Assert.NotNull(media);
        Assert.Equal("photo", media.Type);
    }

    [Fact]
    public void ResolveMedia_WithNullMediaKey_ReturnsNull()
    {
        // Arrange
        var includes = new Includes { Media = new List<Media> { new Media { MediaKey = "media_1" } } };

        // Act
        var media = includes.ResolveMedia((string?)null);

        // Assert
        Assert.Null(media);
    }

    [Fact]
    public void ResolveMedia_WithMediaNotFound_ReturnsNull()
    {
        // Arrange
        var includes = new Includes { Media = new List<Media> { new Media { MediaKey = "media_1" } } };

        // Act
        var media = includes.ResolveMedia("media_2");

        // Assert
        Assert.Null(media);
    }

    #endregion

    #region ResolveMedia (Multiple) Tests

    [Fact]
    public void ResolveMedia_WithMultipleKeys_ReturnsMatchingMedia()
    {
        // Arrange
        var includes = new Includes
        {
            Media = new List<Media>
            {
                new Media { MediaKey = "media_1", Type = "photo" },
                new Media { MediaKey = "media_2", Type = "video" },
                new Media { MediaKey = "media_3", Type = "gif" }
            }
        };
        IEnumerable<string> keys = new[] { "media_1", "media_3" };

        // Act
        var media = includes.ResolveMedia(keys);

        // Assert
        Assert.Equal(2, media.Count);
        Assert.Contains(media, m => m.MediaKey == "media_1");
        Assert.Contains(media, m => m.MediaKey == "media_3");
    }

    [Fact]
    public void ResolveMedia_WithEmptyKeys_ReturnsEmptyList()
    {
        // Arrange
        var includes = new Includes { Media = new List<Media> { new Media { MediaKey = "media_1" } } };

        // Act
        var media = includes.ResolveMedia(new List<string>());

        // Assert
        Assert.Empty(media);
    }

    [Fact]
    public void ResolveMedia_WithNullKeys_ReturnsEmptyList()
    {
        // Arrange
        var includes = new Includes { Media = new List<Media> { new Media { MediaKey = "media_1" } } };

        // Act
        var media = includes.ResolveMedia((IEnumerable<string>?)null);

        // Assert
        Assert.Empty(media);
    }

    [Fact]
    public void ResolveMedia_WithPartialMatch_ReturnsMatchedOnly()
    {
        // Arrange
        var includes = new Includes
        {
            Media = new List<Media>
            {
                new Media { MediaKey = "media_1" },
                new Media { MediaKey = "media_2" }
            }
        };
        IEnumerable<string> keys = new[] { "media_1", "media_999" };

        // Act
        var media = includes.ResolveMedia(keys);

        // Assert
        Assert.Single(media);
        Assert.Equal("media_1", media[0].MediaKey);
    }

    #endregion

    #region ResolveUsersByUsername Tests

    [Fact]
    public void ResolveUsersByUsername_WithValidUsernames_ReturnsUsers()
    {
        // Arrange
        var includes = new Includes
        {
            Users = new List<User>
            {
                new User { Id = "1", Username = "user1" },
                new User { Id = "2", Username = "user2" },
                new User { Id = "3", Username = "user3" }
            }
        };
        var usernames = new[] { "user1", "user3" };

        // Act
        var users = includes.ResolveUsersByUsername(usernames);

        // Assert
        Assert.Equal(2, users.Count);
        Assert.Contains(users, u => u.Username == "user1");
        Assert.Contains(users, u => u.Username == "user3");
    }

    [Fact]
    public void ResolveUsersByUsername_WithNullUsernames_ReturnsEmptyList()
    {
        // Arrange
        var includes = new Includes { Users = new List<User> { new User { Username = "user1" } } };

        // Act
        var users = includes.ResolveUsersByUsername(null);

        // Assert
        Assert.Empty(users);
    }

    [Fact]
    public void ResolveUsersByUsername_WithNullIncludes_ReturnsEmptyList()
    {
        // Arrange
        Includes? includes = null;

        // Act
        var users = includes.ResolveUsersByUsername(new[] { "user1" });

        // Assert
        Assert.Empty(users);
    }

    [Fact]
    public void ResolveUsersByUsername_CaseSensitive_MatchesExactCase()
    {
        // Arrange
        var includes = new Includes
        {
            Users = new List<User>
            {
                new User { Username = "User1" },
                new User { Username = "user2" }
            }
        };
        var usernames = new[] { "user1" }; // lowercase

        // Act
        var users = includes.ResolveUsersByUsername(usernames);

        // Assert
        Assert.Empty(users); // Should not match "User1" due to case sensitivity
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ExpansionResolver_ResolvesCompleteExpansionScenario()
    {
        // Arrange - simulate a complete response with expansions
        var author = new User { Id = "author_1", Username = "author", Name = "The Author" };
        var mentionedUser = new User { Id = "mentioned_1", Username = "mentioned" };
        var referencedTweet = new Tweet { Id = "ref_1", Text = "Original tweet", AuthorId = "author_1" };
        var media = new Media { MediaKey = "media_1", Type = "photo" };

        var tweet = new Tweet
        {
            Id = "1",
            Text = "Check this out @mentioned",
            AuthorId = "author_1"
        };

        var includes = new Includes
        {
            Users = new List<User> { author, mentionedUser },
            Tweets = new List<Tweet> { referencedTweet },
            Media = new List<Media> { media }
        };

        // Act
        var resolvedAuthor = tweet.ResolveAuthor(includes);
        var resolvedTweet = includes.ResolveTweet("ref_1");
        var resolvedMedia = includes.ResolveMedia("media_1");
        var resolvedUsers = includes.ResolveUsersByUsername(new[] { "author", "mentioned" });

        // Assert
        Assert.NotNull(resolvedAuthor);
        Assert.Equal("author", resolvedAuthor.Username);
        Assert.NotNull(resolvedTweet);
        Assert.Equal("Original tweet", resolvedTweet.Text);
        Assert.NotNull(resolvedMedia);
        Assert.Equal(2, resolvedUsers.Count);
    }

    #endregion
}