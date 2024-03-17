namespace Microbians.Services
{
    public interface ITokenValidationService
    {
        WebResponse<bool> ValidateToken(string token);
        string GenerateToken<T>(T user, IEnumerable<Claim> roles) where T : IdentityUser;
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        (ClaimsPrincipal principle, SecurityToken validated) GetSecurityAndPrinciple(string token);
        string GetRefreshToken(string id);
    }
}