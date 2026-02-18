using System.Text.Json;

namespace Aero.Social.Twitter.Client.Errors;

/// <summary>
/// Parses error responses from the Twitter API.
/// </summary>
public static class ErrorResponseParser
{
    /// <summary>
    /// Parses a JSON error response from the Twitter API.
    /// </summary>
    /// <param name="jsonResponse">The JSON response string.</param>
    /// <returns>A list of parsed Twitter errors.</returns>
    public static IReadOnlyList<TwitterError> ParseErrorResponse(string? jsonResponse)
    {
        var errors = new List<TwitterError>();

        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            return errors;
        }

        try
        {
            using var document = JsonDocument.Parse(jsonResponse);
            var root = document.RootElement;

            // Try to parse v2 API error format
            if (root.TryGetProperty("errors", out var errorsArray) && errorsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var errorElement in errorsArray.EnumerateArray())
                {
                    var error = ParseV2Error(errorElement);
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                }
            }
            // Try to parse v1 API error format
            else if (root.TryGetProperty("error", out var errorProperty))
            {
                var error = ParseV1Error(root);
                if (error != null)
                {
                    errors.Add(error);
                }
            }
            // Try alternate v1 format with errors array
            else if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Object)
            {
                var error = ParseV1ErrorObject(errorsProp);
                if (error != null)
                {
                    errors.Add(error);
                }
            }
        }
        catch (JsonException)
        {
            // If we can't parse the JSON, return an empty list
        }

        return errors;
    }

    /// <summary>
    /// Parses a single error from a v2 API response element.
    /// </summary>
    private static TwitterError? ParseV2Error(JsonElement element)
    {
        var error = new TwitterError();

        // Parse required fields
        if (element.TryGetProperty("message", out var messageProp))
        {
            error.Message = messageProp.GetString() ?? string.Empty;
        }

        if (element.TryGetProperty("code", out var codeProp) && codeProp.TryGetInt32(out var code))
        {
            error.Code = code;
        }
        else if (element.TryGetProperty("type", out var typeProp))
        {
            // Some v2 errors use 'type' instead of 'code'
            error.Type = typeProp.GetString() ?? string.Empty;
        }

        // Parse optional fields
        if (element.TryGetProperty("field", out var fieldProp))
        {
            error.Field = fieldProp.GetString();
        }

        if (element.TryGetProperty("resource_id", out var resourceIdProp))
        {
            error.ResourceId = resourceIdProp.GetString();
        }

        if (element.TryGetProperty("resource_type", out var resourceTypeProp))
        {
            error.ResourceType = resourceTypeProp.GetString();
        }

        // Add documentation URL based on error code
        if (error.Code > 0)
        {
            error.DocumentationUrl = TwitterErrorInfo.GetDocumentationUrl(error.Code);
        }

        return error;
    }

    /// <summary>
    /// Parses a v1 API error response.
    /// </summary>
    private static TwitterError? ParseV1Error(JsonElement root)
    {
        var error = new TwitterError();

        // Parse error message
        if (root.TryGetProperty("error", out var errorProp))
        {
            error.Message = errorProp.GetString() ?? string.Empty;
        }

        // Try to parse error code from message or separate field
        if (root.TryGetProperty("code", out var codeProp) && codeProp.TryGetInt32(out var code))
        {
            error.Code = code;
        }
        else
        {
            // Try to extract code from message
            error.Code = ExtractErrorCodeFromMessage(error.Message);
        }

        // Add documentation URL
        if (error.Code > 0)
        {
            error.DocumentationUrl = TwitterErrorInfo.GetDocumentationUrl(error.Code);
        }

        return error;
    }

    /// <summary>
    /// Parses a v1 API error object.
    /// </summary>
    private static TwitterError? ParseV1ErrorObject(JsonElement errorsElement)
    {
        var error = new TwitterError();

        // The errors object might have error codes as keys
        foreach (var property in errorsElement.EnumerateObject())
        {
            if (int.TryParse(property.Name, out var code))
            {
                error.Code = code;
                error.Message = property.Value.GetString() ?? string.Empty;
                error.DocumentationUrl = TwitterErrorInfo.GetDocumentationUrl(code);
                break;
            }
        }

        return error;
    }

    /// <summary>
    /// Attempts to extract an error code from an error message.
    /// </summary>
    private static int ExtractErrorCodeFromMessage(string message)
    {
        // Common error code extraction patterns
        if (message.Contains("Could not authenticate you", StringComparison.OrdinalIgnoreCase))
            return 32;
        if (message.Contains("Sorry, that page does not exist", StringComparison.OrdinalIgnoreCase))
            return 34;
        if (message.Contains("Rate limit exceeded", StringComparison.OrdinalIgnoreCase))
            return 88;
        if (message.Contains("No status found", StringComparison.OrdinalIgnoreCase))
            return 144;
        if (message.Contains("Status is a duplicate", StringComparison.OrdinalIgnoreCase))
            return 187;
        if (message.Contains("Bad authentication data", StringComparison.OrdinalIgnoreCase))
            return 215;

        return 0;
    }

    /// <summary>
    /// Gets the primary error message from a list of errors.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <returns>The primary error message, or a default message if no errors exist.</returns>
    public static string GetPrimaryErrorMessage(IReadOnlyList<TwitterError> errors)
    {
        if (errors.Count == 0)
        {
            return "An unknown error occurred.";
        }

        var primaryError = errors[0];
            
        if (primaryError.Code > 0)
        {
            return TwitterErrorInfo.BuildEnhancedMessage(primaryError.Code, primaryError.Message);
        }

        return primaryError.Message;
    }

    /// <summary>
    /// Builds a comprehensive error message from multiple errors.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <returns>A comprehensive error message.</returns>
    public static string BuildComprehensiveErrorMessage(IReadOnlyList<TwitterError> errors)
    {
        if (errors.Count == 0)
        {
            return "An unknown error occurred.";
        }

        if (errors.Count == 1)
        {
            return GetPrimaryErrorMessage(errors);
        }

        var messages = new List<string>();
        messages.Add($"Multiple errors occurred ({errors.Count}):");
        messages.Add("");

        for (int i = 0; i < errors.Count; i++)
        {
            var error = errors[i];
            var prefix = $"{i + 1}. ";
                
            if (error.Code > 0)
            {
                messages.Add($"{prefix}Error {error.Code}: {TwitterErrorInfo.GetErrorTitle(error.Code)}");
                if (!string.IsNullOrEmpty(error.Message))
                {
                    messages.Add($"   Message: {error.Message}");
                }
                if (!string.IsNullOrEmpty(error.Field))
                {
                    messages.Add($"   Field: {error.Field}");
                }
            }
            else
            {
                messages.Add($"{prefix}{error.Message}");
            }
        }

        messages.Add("");
        messages.Add("For more information, visit: https://developer.twitter.com/en/docs/twitter-api/v1/troubleshooting");

        return string.Join("\n", messages);
    }
}