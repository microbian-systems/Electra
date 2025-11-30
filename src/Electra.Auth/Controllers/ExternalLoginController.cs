using System.Linq;
using Electra.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using Electra.Web.Core.Controllers;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Electra.Auth.Controllers;

[Route("api/[controller]")]
public class ExternalLoginController(
    UserManager<ElectraUser> userManager,
    SignInManager<ElectraUser> signInManager,
    ILogger<ExternalLoginController> logger)
    : ElectraApiBaseController(logger)
{
    /// <summary>
    /// Gets available external login providers
    /// </summary>
    [HttpGet("providers")]
    public async Task<IActionResult> GetExternalProviders()
    {
        var schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
        var providers = schemes.Select(s => new 
        { 
            Name = s.Name,
            DisplayName = s.DisplayName 
        }).ToList();

        return Ok(providers);
    }

    /// <summary>
    /// Initiates external login flow
    /// </summary>
    [HttpGet("challenge/{provider}")]
    public IActionResult Challenge(string provider, string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(provider))
        {
            return BadRequest("Provider is required");
        }

        // Validate the provider exists
        var availableProviders = HttpContext.RequestServices
            .GetRequiredService<IAuthenticationSchemeProvider>()
            .GetAllSchemesAsync()
            .Result
            .Where(s => !string.IsNullOrEmpty(s.DisplayName))
            .Select(s => s.Name);

        if (!availableProviders.Contains(provider))
        {
            return BadRequest($"Provider '{provider}' is not available");
        }

        var redirectUrl = Url.Action(nameof(Callback), new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        
        return Challenge(properties, provider);
    }

    /// <summary>
    /// Handles external login callback
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string? returnUrl = null, string? remoteError = null)
    {
        if (!string.IsNullOrEmpty(remoteError))
        {
            logger.LogWarning("External login error: {Error}", remoteError);
            return BadRequest($"External login error: {remoteError}");
        }

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            logger.LogWarning("External login info not available");
            return BadRequest("External login failed");
        }

        try
        {
            // Check if user already exists with this external login
            var existingUser = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            
            if (existingUser != null)
            {
                // User exists, sign them in
                return await CreateTokenResponse(existingUser);
            }

            // Check if user exists by email
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrEmpty(email))
            {
                var userByEmail = await userManager.FindByEmailAsync(email);
                if (userByEmail != null)
                {
                    // Link this external login to the existing user
                    var linkResult = await userManager.AddLoginAsync(userByEmail, info);
                    if (linkResult.Succeeded)
                    {
                        logger.LogInformation("Linked {Provider} login to existing user {Email}", 
                            info.LoginProvider, email);
                        return await CreateTokenResponse(userByEmail);
                    }
                    else
                    {
                        logger.LogError("Failed to link external login to user {Email}: {Errors}", 
                            email, string.Join(", ", linkResult.Errors.Select(e => e.Description)));
                        return BadRequest("Failed to link external login to existing account");
                    }
                }
            }

            // Create new user
            var newUser = await CreateUserFromExternalLogin(info);
            if (newUser == null)
            {
                return BadRequest("Failed to create user from external login");
            }

            return await CreateTokenResponse(newUser);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing external login callback");
            return StatusCode(500, "Error processing external login");
        }
    }

    /// <summary>
    /// Links an external provider to the current user's account
    /// </summary>
    [HttpPost("link/{provider}")]
    public async Task<IActionResult> LinkProvider(string provider)
    {
        if (!User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized();
        }

        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found");
        }

        var redirectUrl = Url.Action(nameof(LinkCallback));
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, userId);
        
        return Challenge(properties, provider);
    }

    /// <summary>
    /// Handles external provider linking callback
    /// </summary>
    [HttpGet("link/callback")]
    public async Task<IActionResult> LinkCallback()
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return BadRequest("External login info not available");
        }

        var userId = info.AuthenticationProperties?.Items["XsrfId"];
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("Invalid state");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found");
        }

        var result = await userManager.AddLoginAsync(user, info);
        if (result.Succeeded)
        {
            logger.LogInformation("Successfully linked {Provider} to user {UserId}", 
                info.LoginProvider, userId);
            return Ok(new { Message = "Provider linked successfully" });
        }

        return BadRequest($"Failed to link provider: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    /// <summary>
    /// Unlinks an external provider from the current user's account
    /// </summary>
    [HttpDelete("unlink/{provider}")]
    public async Task<IActionResult> UnlinkProvider(string provider)
    {
        if (!User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized();
        }

        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found");
        }

        var logins = await userManager.GetLoginsAsync(user);
        var loginToRemove = logins.FirstOrDefault(l => l.LoginProvider == provider);
        
        if (loginToRemove == null)
        {
            return BadRequest("Provider not linked to this account");
        }

        // Ensure user has a password or other login method
        var hasPassword = await userManager.HasPasswordAsync(user);
        var otherLogins = logins.Where(l => l.LoginProvider != provider).ToList();
        
        if (!hasPassword && otherLogins.Count == 0)
        {
            return BadRequest("Cannot remove the only login method. Please set a password first.");
        }

        var result = await userManager.RemoveLoginAsync(user, loginToRemove.LoginProvider, loginToRemove.ProviderKey);
        if (result.Succeeded)
        {
            logger.LogInformation("Successfully unlinked {Provider} from user {UserId}", 
                provider, userId);
            return Ok(new { Message = "Provider unlinked successfully" });
        }

        return BadRequest($"Failed to unlink provider: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    private async Task<ElectraUser?> CreateUserFromExternalLogin(ExternalLoginInfo info)
    {
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname);
        var name = info.Principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email))
        {
            logger.LogWarning("External login {Provider} did not provide an email", info.LoginProvider);
            return null;
        }

        var user = new ElectraUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true, // Trust external provider's email verification
            Profile = new ElectraUserProfile
            {
               
            }
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            logger.LogError("Failed to create user from external login: {Errors}", 
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return null;
        }

        var loginResult = await userManager.AddLoginAsync(user, info);
        if (!loginResult.Succeeded)
        {
            logger.LogError("Failed to add external login to user: {Errors}", 
                string.Join(", ", loginResult.Errors.Select(e => e.Description)));
            
            // Cleanup - delete the user we just created
            await userManager.DeleteAsync(user);
            return null;
        }

        // Add default role
        await userManager.AddToRoleAsync(user, "User");

        logger.LogInformation("Created new user {Email} from {Provider} external login", 
            email, info.LoginProvider);

        return user;
    }

    private async Task<IActionResult> CreateTokenResponse(ElectraUser user)
    {
        // Create claims identity for OpenIddict
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(Claims.Subject, user.Id.ToString()));
        identity.AddClaim(new Claim(Claims.Email, user.Email ?? string.Empty));
        identity.AddClaim(new Claim(Claims.Name, user.UserName ?? string.Empty));
        //identity.AddClaim(new Claim(Claims.AuthenticationMethod, "external")); // todo - find out if the external login claim is needed

        // Add roles as claims
        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(Claims.Role, role));
        }

        var principal = new ClaimsPrincipal(identity);
        
        // Set scopes
        principal.SetScopes(new[]
        {
            Scopes.OpenId,
            Scopes.Email,
            Scopes.Profile,
            Scopes.OfflineAccess,
            "api",
            "roles"
        });

        // Set resources (audience)
        principal.SetResources("electra-api");

        logger.LogInformation("User {Email} authenticated successfully via external login", user.Email);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}