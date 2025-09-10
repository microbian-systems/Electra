namespace Electra.Models;

public class AuthResponse
{
    public string accessToken { get; set; }
    public string refreshToken { get; set; }
    public DateTimeOffset Expiration { get; set; }

    public AuthResponse(string accessToken, string refreshToken, DateTimeOffset expiration)
    {
        this.accessToken = accessToken;
        this.refreshToken = refreshToken;
        Expiration = expiration;
    }
}