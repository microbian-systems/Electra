namespace Electra.Models;

public record ApiKeyAuthRequestModel
{
    [JsonPropertyName("api_key")] 
    public string ApiKey { get; init; } = "";
}