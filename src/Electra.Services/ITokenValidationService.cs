using System.Collections.Generic;
using System.Security.Claims;
using Electra.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Electra.Services;

public interface ITokenValidationService
{
    WebResponse<bool> ValidateToken(string token);
    string GenerateToken<T>(T user, IEnumerable<Claim> roles) where T : IdentityUser;
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    (ClaimsPrincipal principle, SecurityToken validated) GetSecurityAndPrinciple(string token);
    string GetRefreshToken(string id);
}