namespace Aero.Models;

public record RefreshTokenResponse
{
    [JsonPropertyName("access_token")] 
    public string? AccessToken { get; set; }
    
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("Expiration")]
    public DateTimeOffset? Expiration { get; set; }
}