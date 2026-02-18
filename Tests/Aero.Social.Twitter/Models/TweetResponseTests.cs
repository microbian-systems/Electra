using System.Text.Json;
using Aero.Social.Twitter.Client.Models;

namespace Aero.Social.Twitter.Models;

public class TweetResponseTests
{
    [Fact]
    public void TweetResponse_Deserialization_WithSingleTweet_PopulatesCorrectly()
    {
        // Arrange
        var json = @"{
                ""data"": [
                    {
                        ""id"": ""1234567890"",
                        ""text"": ""Hello, World!"",
                        ""created_at"": ""2020-01-01T00:00:00.000Z"",
                        ""author_id"": ""9876543210""
                    }
                ],
                ""meta"": {
                    ""result_count"": 1,
                    ""newest_id"": ""1234567890"",
                    ""oldest_id"": ""1234567890""
                }
            }";

        // Act
        var response = JsonSerializer.Deserialize<TweetResponse>(json);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);
        Assert.Equal("1234567890", response.Data[0].Id);
        Assert.Equal("Hello, World!", response.Data[0].Text);
        Assert.NotNull(response.Meta);
        Assert.Equal(1, response.Meta.ResultCount);
    }

    [Fact]
    public void TweetResponse_Deserialization_WithMultipleTweets_PopulatesCorrectly()
    {
        // Arrange
        var json = @"{
                ""data"": [
                    {
                        ""id"": ""1234567890"",
                        ""text"": ""First tweet"",
                        ""created_at"": ""2020-01-02T00:00:00.000Z""
                    },
                    {
                        ""id"": ""1234567891"",
                        ""text"": ""Second tweet"",
                        ""created_at"": ""2020-01-01T00:00:00.000Z""
                    }
                ],
                ""meta"": {
                    ""result_count"": 2,
                    ""next_token"": ""next_page_token"",
                    ""newest_id"": ""1234567890"",
                    ""oldest_id"": ""1234567891""
                }
            }";

        // Act
        var response = JsonSerializer.Deserialize<TweetResponse>(json);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.Count);
        Assert.NotNull(response.Meta);
        Assert.Equal("next_page_token", response.Meta.NextToken);
    }

    [Fact]
    public void TweetResponse_Deserialization_WithPaginationTokens_PopulatesCorrectly()
    {
        // Arrange
        var json = @"{
                ""data"": [],
                ""meta"": {
                    ""result_count"": 0,
                    ""next_token"": ""b26v89c19zqg8o3f"",
                    ""previous_token"": ""a12v78c18ypf7n2e""
                }
            }";

        // Act
        var response = JsonSerializer.Deserialize<TweetResponse>(json);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Meta);
        Assert.Equal("b26v89c19zqg8o3f", response.Meta.NextToken);
        Assert.Equal("a12v78c18ypf7n2e", response.Meta.PreviousToken);
    }
}