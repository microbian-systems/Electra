using Aero.Social.Twitter.Client.Models;

namespace Aero.Social.Twitter.Models;

public class UserFieldsTests
{
    [Fact]
    public void UserFields_ToApiString_WithSingleField_ReturnsCorrectString()
    {
        // Arrange
        var fields = UserFields.Description;

        // Act
        var result = fields.ToApiString();

        // Assert
        Assert.Equal("description", result);
    }

    [Fact]
    public void UserFields_ToApiString_WithMultipleFields_ReturnsCommaSeparatedString()
    {
        // Arrange
        var fields = UserFields.Description | UserFields.Location | UserFields.Verified;

        // Act
        var result = fields.ToApiString();

        // Assert
        Assert.Contains("description", result);
        Assert.Contains("location", result);
        Assert.Contains("verified", result);
        Assert.Contains(",", result);
    }

    [Fact]
    public void UserFields_ToApiString_WithAllFields_ReturnsAllFieldNames()
    {
        // Arrange
        var fields = UserFields.All;

        // Act
        var result = fields.ToApiString();

        // Assert
        Assert.Contains("created_at", result);
        Assert.Contains("description", result);
        Assert.Contains("location", result);
        Assert.Contains("verified", result);
        Assert.Contains("public_metrics", result);
        Assert.Contains("profile_image_url", result);
    }

    [Fact]
    public void UserFields_ToApiString_WithNone_ReturnsEmptyString()
    {
        // Arrange
        var fields = UserFields.None;

        // Act
        var result = fields.ToApiString();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData(UserFields.CreatedAt, "created_at")]
    [InlineData(UserFields.Description, "description")]
    [InlineData(UserFields.Location, "location")]
    [InlineData(UserFields.Verified, "verified")]
    [InlineData(UserFields.PublicMetrics, "public_metrics")]
    [InlineData(UserFields.ProfileImageUrl, "profile_image_url")]
    [InlineData(UserFields.Url, "url")]
    [InlineData(UserFields.Username, "username")]
    public void UserFields_ToApiString_IndividualFields_ReturnCorrectValues(UserFields field, string expected)
    {
        // Act
        var result = field.ToApiString();

        // Assert
        Assert.Equal(expected, result);
    }
}