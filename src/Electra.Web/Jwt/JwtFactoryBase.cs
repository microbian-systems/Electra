using System.Security.Claims;
using System.Security.Cryptography;

namespace Electra.Common.Web.Jwt;

public abstract class JwtFactoryBase(ILogger<JwtFactoryBase> log) : IJwtFactory
{
    protected readonly ILogger<JwtFactoryBase> log = log;

    public abstract JwtResponseModel GenerateAccessToken(List<Claim> claims);
    public abstract ClaimsPrincipal? GetPrincipalFromToken(string? token);
    public abstract bool IsValidToken(string token);

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        
        var token = Convert.ToBase64String(randomNumber);
        return token;
    }
}