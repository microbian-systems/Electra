using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Electra.Core.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Electra.Common.Web.Jwt;


public class JwtEncryptedFactory : JwtFactoryBase, IJwtFactory
{
    private readonly JwtOptions options;
    private readonly byte[] encryptionKey;

    public JwtEncryptedFactory(IOptions<JwtOptions> options, ILogger<JwtEncryptedFactory> log) 
        : base(log)
    {
        this.options = options.Value;
        this.encryptionKey = this.options.EncryptionKey.FromBase64ToBytes();
    }
    
    public override JwtResponseModel GenerateAccessToken(List<Claim> claims)
    {
        log.LogInformation("generating encrypted jwt access token...");
        log.LogDebug("jwt options are: {@options}", ObjectExtensions.ToJson(options));
        
        var expiry = DateTime.UtcNow.AddMinutes(options.ExpiryInMinutes);
        var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));

        var signingCredentials = new SigningCredentials(secret, SecurityAlgorithms.HmacSha512);

        using var provider = Aes.Create();
        provider.KeySize = 128; // Set the key size to 128 bits for AES128 encryption
        var s1 = provider.Key;
        var sk = new SymmetricSecurityKey(s1);
        var encryptingCredentials = new EncryptingCredentials(sk,
            SecurityAlgorithms.Aes128KW,
            SecurityAlgorithms.Aes128CbcHmacSha256);

        var handler = new JwtSecurityTokenHandler();
        var tokenOptions = handler.CreateJwtSecurityToken(new SecurityTokenDescriptor
        {
            Audience = options.Audience,
            Issuer = options.Issuer,
            Subject = new ClaimsIdentity(claims),
            Expires = expiry,
            EncryptingCredentials = encryptingCredentials,
            SigningCredentials = signingCredentials
        });

        var token = handler.WriteToken(tokenOptions);
        log.LogInformation("generated access token: {0}", token);

        var model = new JwtResponseModel()
        {
            AccessToken = token,
            Expiry = expiry,
            RefreshToken = GenerateRefreshToken()
        };

        return model;
    }
    
    public override bool IsValidToken(string token)
    {
        // var symkey = new SymmetricSecurityKey(encryptionKey);
        // var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var symkey = new SymmetricSecurityKey(options.EncryptionKey.FromBase64ToBytes());

        var handler = new JwtSecurityTokenHandler();
        try
        {
            // Token validation and decryption
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = options.Issuer,
                ValidAudience = options.Audience,
                IssuerSigningKey = secret,
                TokenDecryptionKey = symkey
            };

            var claimsPrincipal = handler
                .ValidateToken(token, validationParameters, out var validatedToken);
        }
        catch(Exception ex)
        {
            log.LogError(ex, "failed to validate jwt token (encrypted)");
            return false;
        }
        
        return true;
    }
    public override ClaimsPrincipal? GetPrincipalFromToken(string? token)
    {
        var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var symkey = new SymmetricSecurityKey(options.EncryptionKey.FromBase64ToBytes());
        // Token validation and decryption
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = secret,
            TokenDecryptionKey = symkey
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler
            .ValidateToken(token, validationParameters, out var validatedToken);

        var jwtToken = (JwtSecurityToken)validatedToken;

        return principal;
    }
}