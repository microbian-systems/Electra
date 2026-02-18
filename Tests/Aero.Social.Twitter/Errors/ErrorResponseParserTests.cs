using Aero.Social.Twitter.Client.Errors;

namespace Aero.Social.Twitter.Errors;

public class ErrorResponseParserTests
{
    [Fact]
    public void ParseErrorResponse_V2Format_ReturnsParsedErrors()
    {
        // Arrange
        var json = @"{
                ""errors"": [
                    {
                        ""message"": ""Sorry, that page does not exist"",
                        ""code"": 34,
                        ""field"": ""id""
                    }
                ]
            }";

        // Act
        var errors = ErrorResponseParser.ParseErrorResponse(json);

        // Assert
        Assert.Single(errors);
        Assert.Equal(34, errors[0].Code);
        Assert.Equal("Sorry, that page does not exist", errors[0].Message);
        Assert.Equal("id", errors[0].Field);
        Assert.NotNull(errors[0].DocumentationUrl);
    }

    [Fact]
    public void ParseErrorResponse_V1ErrorFormat_ReturnsParsedError()
    {
        // Arrange
        var json = @"{ ""error"": ""Rate limit exceeded"", ""code"": 88 }";

        // Act
        var errors = ErrorResponseParser.ParseErrorResponse(json);

        // Assert
        Assert.Single(errors);
        Assert.Equal(88, errors[0].Code);
        Assert.Equal("Rate limit exceeded", errors[0].Message);
    }

    [Fact]
    public void ParseErrorResponse_V1ErrorObjectFormat_ReturnsParsedError()
    {
        // Arrange
        var json = @"{ ""errors"": { ""88"": ""Rate limit exceeded"" } }";

        // Act
        var errors = ErrorResponseParser.ParseErrorResponse(json);

        // Assert
        Assert.Single(errors);
        Assert.Equal(88, errors[0].Code);
        Assert.Equal("Rate limit exceeded", errors[0].Message);
    }

    [Fact]
    public void ParseErrorResponse_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var json = @"{
                ""errors"": [
                    {
                        ""message"": ""Sorry, that page does not exist"",
                        ""code"": 34
                    },
                    {
                        ""message"": ""User not found"",
                        ""code"": 50
                    }
                ]
            }";

        // Act
        var errors = ErrorResponseParser.ParseErrorResponse(json);

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.Equal(34, errors[0].Code);
        Assert.Equal(50, errors[1].Code);
    }

    [Fact]
    public void ParseErrorResponse_V2FormatWithResource_ReturnsResourceInfo()
    {
        // Arrange
        var json = @"{
                ""errors"": [
                    {
                        ""message"": ""Could not find user"",
                        ""code"": 50,
                        ""resource_type"": ""user"",
                        ""resource_id"": ""12345""
                    }
                ]
            }";

        // Act
        var errors = ErrorResponseParser.ParseErrorResponse(json);

        // Assert
        Assert.Single(errors);
        Assert.Equal("user", errors[0].ResourceType);
        Assert.Equal("12345", errors[0].ResourceId);
    }

    [Fact]
    public void ParseErrorResponse_InvalidJson_ReturnsEmptyList()
    {
        // Arrange
        var json = "not valid json";

        // Act
        var errors = ErrorResponseParser.ParseErrorResponse(json);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ParseErrorResponse_NullResponse_ReturnsEmptyList()
    {
        // Act
        var errors = ErrorResponseParser.ParseErrorResponse(null);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ParseErrorResponse_EmptyResponse_ReturnsEmptyList()
    {
        // Act
        var errors = ErrorResponseParser.ParseErrorResponse("");

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ParseErrorResponse_NoErrorsField_ReturnsEmptyList()
    {
        // Arrange
        var json = @"{ ""data"": ""some value"" }";

        // Act
        var errors = ErrorResponseParser.ParseErrorResponse(json);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ParseErrorResponse_V1WithErrorCodeInMessage_ExtractsCode()
    {
        // Arrange
        var json = @"{ ""error"": ""Could not authenticate you"" }";

        // Act
        var errors = ErrorResponseParser.ParseErrorResponse(json);

        // Assert
        Assert.Single(errors);
        Assert.Equal(32, errors[0].Code); // Code extracted from message
        Assert.Equal("Could not authenticate you", errors[0].Message);
    }

    [Fact]
    public void GetPrimaryErrorMessage_SingleError_ReturnsEnhancedMessage()
    {
        // Arrange
        var errors = new List<TwitterError>
        {
            new TwitterError { Code = 88, Message = "Rate limit exceeded" }
        };

        // Act
        var message = ErrorResponseParser.GetPrimaryErrorMessage(errors);

        // Assert
        Assert.Contains("Twitter API Error 88", message);
        Assert.Contains("Rate limit exceeded", message);
    }

    [Fact]
    public void GetPrimaryErrorMessage_EmptyList_ReturnsDefaultMessage()
    {
        // Arrange
        var errors = new List<TwitterError>();

        // Act
        var message = ErrorResponseParser.GetPrimaryErrorMessage(errors);

        // Assert
        Assert.Equal("An unknown error occurred.", message);
    }

    [Fact]
    public void GetPrimaryErrorMessage_ErrorWithoutCode_ReturnsMessageOnly()
    {
        // Arrange
        var errors = new List<TwitterError>
        {
            new TwitterError { Code = 0, Message = "Some error without code" }
        };

        // Act
        var message = ErrorResponseParser.GetPrimaryErrorMessage(errors);

        // Assert
        Assert.Equal("Some error without code", message);
    }

    [Fact]
    public void BuildComprehensiveErrorMessage_SingleError_ReturnsEnhancedMessage()
    {
        // Arrange
        var errors = new List<TwitterError>
        {
            new TwitterError { Code = 88, Message = "Rate limit exceeded" }
        };

        // Act
        var message = ErrorResponseParser.BuildComprehensiveErrorMessage(errors);

        // Assert
        Assert.Contains("Twitter API Error 88", message);
    }

    [Fact]
    public void BuildComprehensiveErrorMessage_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var errors = new List<TwitterError>
        {
            new TwitterError { Code = 34, Message = "Not found", Field = "id" },
            new TwitterError { Code = 50, Message = "User not found", Field = "username" }
        };

        // Act
        var message = ErrorResponseParser.BuildComprehensiveErrorMessage(errors);

        // Assert
        Assert.Contains("Multiple errors occurred (2)", message);
        Assert.Contains("1.", message);
        Assert.Contains("Error 34", message);
        Assert.Contains("2.", message);
        Assert.Contains("Error 50", message);
        Assert.Contains("Field: id", message);
        Assert.Contains("Field: username", message);
        Assert.Contains("developer.twitter.com", message);
    }

    [Fact]
    public void BuildComprehensiveErrorMessage_EmptyList_ReturnsDefaultMessage()
    {
        // Arrange
        var errors = new List<TwitterError>();

        // Act
        var message = ErrorResponseParser.BuildComprehensiveErrorMessage(errors);

        // Assert
        Assert.Equal("An unknown error occurred.", message);
    }
}