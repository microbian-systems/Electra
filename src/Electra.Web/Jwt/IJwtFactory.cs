using System.Security.Claims;

namespace Electra.Common.Web.Jwt;

public record JwtResponseModel
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTimeOffset Expiry { get; set; }
    public DateTimeOffset RefreshExpiry { get; set; }
}

public interface IJwtFactory
{
    JwtResponseModel GenerateAccessToken(List<Claim> claims);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromToken(string? token);
    bool IsValidToken(string token);
}