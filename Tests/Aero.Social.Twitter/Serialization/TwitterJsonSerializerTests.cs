using Aero.Social.Twitter.Client.Serialization;

namespace Aero.Social.Twitter.Serialization;

public class TwitterJsonSerializerTests
{
    [Fact]
    public void Deserialize_ShouldParseValidJson()
    {
        // Arrange
        var json = @"{""id"": ""123"", ""text"": ""Hello""}";

        // Act
        var result = TwitterJsonSerializer.Deserialize<TestModel>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("123", result.Id);
        Assert.Equal("Hello", result.Text);
    }

    [Fact]
    public void Deserialize_ShouldHandleSnakeCase()
    {
        // Arrange
        var json = @"{""created_at"": ""2024-01-15T10:30:00.000Z"", ""author_id"": ""456""}";

        // Act
        var result = TwitterJsonSerializer.Deserialize<TestModel>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("456", result.AuthorId);
    }

    [Fact]
    public void Serialize_ShouldOutputSnakeCase()
    {
        // Arrange
        var model = new TestModel
        {
            Id = "123",
            Text = "Hello",
            AuthorId = "456"
        };

        // Act
        var json = TwitterJsonSerializer.Serialize(model);

        // Assert
        Assert.Contains("\"id\"", json);
        Assert.Contains("\"text\"", json);
        Assert.Contains("\"author_id\"", json);
    }

    [Fact]
    public void Serialize_ShouldSkipNullValues()
    {
        // Arrange
        var model = new TestModel
        {
            Id = "123",
            Text = null,
            AuthorId = "456"
        };

        // Act
        var json = TwitterJsonSerializer.Serialize(model);

        // Assert
        Assert.Contains("\"id\"", json);
        Assert.Contains("\"author_id\"", json);
    }

    private class TestModel
    {
        public string? Id { get; set; }
        public string? Text { get; set; }
        public string? AuthorId { get; set; }
    }
}