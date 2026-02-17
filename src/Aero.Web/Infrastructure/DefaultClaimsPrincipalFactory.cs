using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aero.Core.Extensions;

namespace Aero.Common.Web.Infrastructure;

/// <summary>
/// Service used to generate a <see cref="ClaimsPrincipal"/>.
/// </summary>
public interface IClaimsPrincipalFactory
{
    /// <summary>
    /// Creates a claims principal to use for authenticating a request.
    /// </summary>
    /// <param name="apiKeyOwnerId">The ID (i.e. - email) of the owner of the API Key.</param>
    /// <param name="apiKey">the apiKey to be included in the principle. If one is not provided a new key is generated</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> that represents the entity that initiated the HTTP request.</returns>
    Task<ClaimsPrincipal> CreateClaimsPrincipal(string apiKeyOwnerId, string? apiKey = null);
}

/// <summary>
/// Default implementation for the <see cref="IClaimsPrincipalFactory"/> service.
/// Creates a principal with the single claim of the owner ID.
/// </summary>
public sealed class ClaimsPrincipalFactory : IClaimsPrincipalFactory
{
    private readonly IApiKeyFactory apiKeyFactory;
    private readonly ILogger<ClaimsPrincipalFactory> log;

    public ClaimsPrincipalFactory(IApiKeyFactory apiKeyFactory, ILogger<ClaimsPrincipalFactory> log)
    {
        this.apiKeyFactory = apiKeyFactory;
        this.log = log;
    }
    
    public Task<ClaimsPrincipal> CreateClaimsPrincipal(string apiKeyOwnerId, string? apiKey = null)
    {
        if (string.IsNullOrEmpty(apiKey))
            apiKey = apiKeyFactory.GenerateApiKey();
        
        var claims = new[]
        {
            new Claim("Id", apiKey!),
            new Claim(ClaimTypes.Name, apiKeyOwnerId),
            new Claim(JwtRegisteredClaimNames.Sub, apiKeyOwnerId),
            new Claim(JwtRegisteredClaimNames.Email, apiKeyOwnerId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        var identity = new ClaimsIdentity(claims, "ApiKey");
        var identities = new List<ClaimsIdentity> { identity };
        var principal = new ClaimsPrincipal(identities);

        log.LogDebug("created claims principle w/ the following claims: {0}", claims.ToJson());
        return Task.FromResult(principal);
    }
}