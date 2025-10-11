using System.Linq;
using Electra.Auth.Models;
using Electra.Common.Web;
using Electra.Models;
using Electra.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Electra.Auth.Controllers;

[Route("api/auth/[controller]")]
public class ExternalLoginController(
    SignInManager<ElectraUser> signInManager,
    UserManager<ElectraUser> userManager,
    ILogger<ExternalLoginController> logger)
    : ApiControllerBase(logger)
{
    [HttpGet("providers")]
    public async Task<IActionResult> GetExternalLoginProviders()
    {
        var providers = (await signInManager.GetExternalAuthenticationSchemesAsync())
            .Select(p => new
            {
                p.Name, p.DisplayName
            })
            .ToList();

        return Ok(providers);
    }

    [HttpPost("challenge")]
    public IActionResult Challenge([FromBody] string provider, [FromQuery] string returnUrl = null)
    {
        // Request a redirect to the external login provider
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "ExternalLogin", new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");

        if (remoteError != null)
        {
            return BadRequest($"Error from external provider: {remoteError}");
        }

        // Get the login information about the user from the external login provider
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return BadRequest("Error loading external login information.");
        }

        ElectraUser? user;

        // Sign in the user with this external login provider if the user already has a login
        var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey,
            isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            logger.LogInformation("{Name} logged in with {LoginProvider} provider", info.Principal.Identity.Name,
                info.LoginProvider);

            // Generate JWT token for the user
            user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

            // Redirect to client application with token
            return Redirect($"{returnUrl}?code={await GenerateJwtToken(user)}");
        }

        // If the user does not have an account, create one
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (email == null)
        {
            return BadRequest("Email claim not found from external provider.");
        }

        user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ElectraUser
            {
                UserName = email,
                Email = email,
                FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName),
                LastName = info.Principal.FindFirstValue(ClaimTypes.Surname)
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return BadRequest(createResult.Errors);
            }

            // Assign the user to the default role
            await userManager.AddToRoleAsync(user, "User");
        }

        // Add the external login to the user
        var addLoginResult = await userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded)
        {
            return BadRequest(addLoginResult.Errors);
        }

        // Sign in the user
        await signInManager.SignInAsync(user, isPersistent: false);

        // Redirect to client application with token
        return Redirect($"{returnUrl}?code={await GenerateJwtToken(user)}");
    }

private async Task<string> GenerateJwtToken(ElectraUser user)
{
    // Create a unique authentication ticket for this external login
    var principal = await signInManager.CreateUserPrincipalAsync(user);
    
    // Add the required claims
    principal.SetClaim(Claims.Subject, await userManager.GetUserIdAsync(user));
    principal.SetClaim(Claims.Name, await userManager.GetUserNameAsync(user));
    principal.SetClaim(Claims.Email, await userManager.GetEmailAsync(user));
    
    // Add roles
    foreach (var role in await userManager.GetRolesAsync(user))
    {
        principal.SetClaim(Claims.Role, role);
    }
    
    // Set scopes
    principal.SetScopes(new[]
    {
        Scopes.OpenId,
        Scopes.Email,
        Scopes.Profile,
        Scopes.OfflineAccess,
        "api"
    });
    
    // Configure destinations for claims
    principal.SetDestinations(GetDestinations);
    
    try
    {
        // Get necessary services
        var authorizationManager = HttpContext.RequestServices.GetRequiredService<IOpenIddictAuthorizationManager>();
        var tokenManager = HttpContext.RequestServices.GetRequiredService<IOpenIddictTokenManager>();
        
        // Determine the client ID from the return URL
        var clientId = DetermineClientId(HttpContext.Request.Query["returnUrl"].ToString());
        
        // Create an authorization
        var authorization = await authorizationManager.CreateAsync(
            principal: principal,
            subject: await userManager.GetUserIdAsync(user),
            client: clientId,
            type: AuthorizationTypes.AdHoc,
            scopes: principal.GetScopes());
        
        // Get authorization ID
        var authorizationId = await authorizationManager.GetIdAsync(authorization);
        
        // Create token descriptor
        var tokenDescriptor = new OpenIddictTokenDescriptor
        {
            AuthorizationId = authorizationId,
            CreationDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddMinutes(5), // Short-lived code
            Principal = principal,
            Status = Statuses.Valid,
            Subject = await userManager.GetUserIdAsync(user),
            Type = TokenTypeHints.AccessToken
        };
        
        // Create token
        var token = await tokenManager.CreateAsync(tokenDescriptor);
        
        // Return token value
        var hash =  await tokenManager.GetPayloadAsync(token);
        return hash;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error generating authorization code for user {UserId}", user.Id);
        throw;
    }
}

private string DetermineClientId(string returnUrl)
{
    // Determine the client ID based on the return URL
    if (string.IsNullOrEmpty(returnUrl))
        return "default_client_id";
        
    if (returnUrl.Contains("localhost:5001") || returnUrl.Contains("web.electra"))
        return "electra_web_client";
    else if (returnUrl.Contains("electra://") || returnUrl.Contains("mobile.electra"))
        return "electra_mobile_client";
    else if (returnUrl.Contains("desktop.electra"))
        return "electra_desktop_client";
    
    // Default client ID
    return "electra_web_client";
}

private static IEnumerable<string> GetDestinations(Claim claim)
{
    // Same as in Approach 1
    switch (claim.Type)
    {
        case Claims.Name:
        case Claims.Email:
        case Claims.Role:
            yield return Destinations.AccessToken;
            yield return Destinations.IdentityToken;
            break;
            
        case "AspNet.Identity.SecurityStamp":
            yield break;
            
        default:
            yield return Destinations.AccessToken;
            break;
    }
}
}