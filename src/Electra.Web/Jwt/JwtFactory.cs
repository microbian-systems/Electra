using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Electra.Core.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Electra.Common.Web.Jwt;

public class JwtFactory : JwtFactoryBase, IJwtFactory
{
    private readonly JwtOptions options;

    public JwtFactory(IOptions<JwtOptions> options, ILogger<JwtFactory> log)
        : base(log)
    {
        this.options = options.Value;
    }
    
    // todo - use of type Tuple<,> is not recommended due to positional arguments breaking changes if args are added or order is changed
    // use the newer Tuple type (DateTimeOffset expiry, string token) instead
    public override JwtResponseModel GenerateAccessToken(List<Claim> claims)
    {
        log.LogInformation("generating jwt access token...");
        log.LogDebug("jwt options are: {@options}", options.ToJson());
        
        var issuer = options.Issuer;
        var audience = options.Audience;
        var key = Encoding.UTF8.GetBytes(options.Key);
        var expiry = DateTime.UtcNow.AddMinutes(options.ExpiryInMinutes);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiry,
            Issuer = issuer,
            Audience = audience,
            NotBefore = DateTime.UtcNow.AddHours(-6),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha512Signature)
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);
        
        log.LogDebug("token: {jwt}", jwt);

        return new JwtResponseModel()
        {
            AccessToken = jwt,
            Expiry = expiry,
            RefreshToken = GenerateRefreshToken()
        };
    }
    
    public override bool IsValidToken(string token)
    {
        var mySecret = options.Key;
        var mySecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(mySecret));

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = options.Issuer,
                ValidAudience = options.Audience,
                IssuerSigningKey = mySecurityKey
            }, out var validatedToken);
        }
        catch
        {
            return false;
        }
        return true;
    }

    public override ClaimsPrincipal? GetPrincipalFromToken(string? token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        
        if (!(securityToken is JwtSecurityToken jwtSecurityToken
            && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512,
                StringComparison.InvariantCultureIgnoreCase)))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }
}