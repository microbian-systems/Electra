using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Electra.Common;
using Electra.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Electra.Services;

public class ElectraJwtValidationService(
    IOptions<AppSettings> settings,
    ILogger<ElectraJwtValidationService> log)
    : ITokenValidationService
{
    public readonly AppSettings settings = settings.Value;


    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();

        rng.GetBytes(randomNumber);

        return Convert.ToBase64String(randomNumber);
    }

    public string GenerateToken<T>(T user, IEnumerable<Claim> roles) where T : IdentityUser
    {
        var claims = new ClaimsIdentity(new Claim[]
        {
            new(ClaimTypes.Name, user.UserName),
            new("id", user.Id.ToString()),
            new(ClaimTypes.Email, user.Email)
        });

        //foreach(var role in roles)
        claims.AddClaims(roles);
        //claims.AddClaim(new Claim(ClaimTypes.Role, role));

        // authentication successful so generate jwt token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(settings.Secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claims,
            Issuer = settings.DomainName,
            Audience = "api://default",
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);
        return jwt;
    }

    public WebResponse<bool> ValidateToken(string token)
    {
        var message = "token is valid";

        var result = new WebResponse<bool>()
        {
            Message = message
        };

        try
        {
            var data = GetSecurityAndPrinciple(token);

            // todo - validate any other jwt token parameters here - i.e. - claims/exp-date, etc....

            result.Data = true;
        }
        catch (Exception ex)
        {
            log.LogError($"Error on token validation: {ex.Message}");
            message = message.Replace("valid", "invalid");
            result.Message = message;
            result.Data = false;
            result.Errors.Add(new BaseErrorResponse()
            {
                StatusCode = 400.ToString(),
                Details = ex.StackTrace,
                Field = "token",
                Message = ex.Message
            });
        }

        return result;
    }

    public (ClaimsPrincipal principle, SecurityToken validated) GetSecurityAndPrinciple(string token)
    {
        var jwtkey = Encoding.UTF8.GetBytes(settings?.Secret);
        var handler = new JwtSecurityTokenHandler();
        var principle = handler.ValidateToken(token, new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true,
            // Clock skew compensates for server time drift.
            // We recommend 5 minutes or less:
            ClockSkew = TimeSpan.FromMinutes(5),
            // Specify the key used to sign the token:
            IssuerSigningKey = new SymmetricSecurityKey(jwtkey),
            RequireSignedTokens = true,
            // Ensure the token hasn't expired:
            RequireExpirationTime = true,
            ValidateLifetime = true,
            // Ensure the token audience matches our audience value (default true):
            ValidateAudience = false,
            ValidAudience = "api://default",
            // todo - validate issuer && audience in jwt
            // Ensure the token was issued by a trusted authorization server (default true):
            ValidateIssuer = true,
            ValidIssuer = settings.DomainName // $"https://{validIssuers}/oauth2/default"
        }, out var validated);
        return (principle, validated);
    }

    public string GetRefreshToken(string id)
    {
        return ""; // todo - implement ElectraTokenValidator.GetRefreshToken() method
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false, //you might want to validate the audience and issuer depending on your use case
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        "the server key used to sign the JWT token is here, use more than 16 chars")),
            ValidateLifetime = false //here we are saying that we don't care about the token's expiration date
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;
        if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }
}