using System.Security.Claims;
using Aero.Common.Web.Infrastructure;
using Aero.Common.Web.Jwt;
using Aero.Common.Web.Services;
using Aero.Core.Extensions;
using Aero.Models;
using Aero.Web.Core.Controllers;

namespace Aero.Common.Web.Controllers;

[Route("/api")]
public sealed class JwtAuthController : AeroApiBaseController
{
    private readonly IApiKeyService apiService;
    private readonly IJwtFactory jwtGenerator;
    private readonly IClaimsPrincipalFactory claimsFactory;

    public JwtAuthController(
        IApiKeyService apiService,
        IClaimsPrincipalFactory claimsFactory,
        IJwtFactory jwtGenerator,
        ILogger<JwtAuthController> log) 
        : base(log)
    {
        this.apiService = apiService;
        this.jwtGenerator = jwtGenerator;
        this.claimsFactory = claimsFactory;
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("register")]
    public async Task<IActionResult> Register(ApiRegistrationRequest request)
    {
        if (string.IsNullOrEmpty(request?.Email)) // todo - validate the email address properly
            return BadRequest("Invalid request");

        var model = await apiService.Register(request);

        return Ok(new { api_key = model.ApiKey});
    }

    [AllowAnonymous]
    [HttpPost("auth")]
    
    public async Task<IActionResult> Authenticate(ApiKeyAuthRequestModel model)
    {
        try
        {
            var message = $"bad auth request for {model.ToJson()}";

            if (string.IsNullOrEmpty(model.ApiKey))
                return BadRequest();

            var account = await apiService.Authenticate(model);

            if (account == null)
            {
                log.LogInformation(message);
                return Unauthorized();
            }

            var principle = await apiService.Authenticate(model);
            var claims = principle.Claims
                .Select(x => new Claim(x.ClaimKey, x.ClaimValue))
                .ToList();
            claims.Add(new Claim(ClaimTypes.Role, "Server"));
            var jwt = jwtGenerator.GenerateAccessToken(claims);
            var refresh = string.IsNullOrEmpty(account.RefreshToken)
                ? jwtGenerator.GenerateRefreshToken()
                : account.RefreshToken;

            return Ok(new AuthResponse(jwt.AccessToken, refresh, jwt.Expiry));
        }
        catch (Exception ex)
        {
            return Content(ex.ToString());
        }
    }
    
    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request?.AccessToken) || string.IsNullOrEmpty(request?.RefreshToken))
        {
            return BadRequest("Invalid client request");
        }

        const string message = "Invalid access / refresh token";

        var refreshed = apiService.TryGetRefreshToken(request, out var newToken);

        if (!refreshed)
            return BadRequest(new ApiAuthResponse() { Message = message });

        return Ok(newToken);
    }
    
    [Authorize]
    [HttpPost("revoke/{apiKey}")]
    public async Task<IActionResult> Revoke(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return BadRequest();
        
        await apiService.Revoke(apiKey);
        
        log.LogInformation($"revoking access to api key {apiKey}");

        return NoContent();
    }

    [HttpPost("testkey")]
    public async Task<IActionResult> Test()
    {
        return Ok();
    }
}