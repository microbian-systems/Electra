namespace Electra.Models;

public record RefreshTokenRequest
{
    [JsonPropertyName("token")]
    public string AccessToken { get; init; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; init; }
}