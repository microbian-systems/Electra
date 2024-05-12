using DotNext;

namespace Electra.Auth;

public static class JwtExtensions
{
    public static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

    public static string ToJwtToken<TKey>(this IdentityUser<TKey> user, string secret, string issuer, string audience,
        TimeSpan? expires = null, List<Claim>? claims = null) where TKey : IEquatable<TKey>
    {
        expires ??= TimeSpan.FromMinutes(15);
        claims ??=
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: new DateTime(expires.Value.Ticks), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static ClaimsPrincipal GetPrincipleFromExpiredToken(this string expiredToken, string secret,
        bool validateAudience = false, bool validateIssuer = false)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = validateAudience,
            ValidateIssuer = validateIssuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateLifetime = false //ignore token's expiration date
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(expiredToken, tokenValidationParameters, out var securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;
        if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
            return null;

        return principal;
    }

    public static double ToUnixEpochExpiration(this DateTime src)
    {
        var unixEpoch = UnixEpoch;
        return Math.Round((src - unixEpoch).TotalSeconds);
    }

    // Method to decode JWT payload from HttpRequest
    public static Result<JwtPayload> DecodeJwtPayload(this HttpRequest request, string secret)
    {
        var token = request.Headers["Authorization"]
            .ToString()?
            .Replace("Bearer ", "") ?? string.Empty;
        return DecodeJwtPayload(token, secret);
    }

    // Method to decode JWT payload from a token string
    public static Result<JwtPayload> DecodeJwtPayload(this string token, string secret)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false // Since we are only decoding, not validating
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            return jwtSecurityToken?.Payload!;
        }
        catch (SecurityTokenMalformedException ex)
        {
            var result = new Result<JwtPayload>(ex);
            return result;
        }
    }
}