using Electra.Auth.Models;
using Electra.Core.Identity;
using Electra.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using Microsoft.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Electra.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ElectraUser> _userManager;
    private readonly SignInManager<ElectraUser> _signInManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ElectraUser> userManager,
        SignInManager<ElectraUser> signInManager,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user account
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = new ElectraUser
        {
            UserName = request.Email,
            Email = request.Email,
            Profile = new ElectraUserProfile
            {
                
            }
        };

        var result = await _userManager.CreateAsync(user, request.Password!);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} created successfully", request.Email);
            
            // Add default role
            await _userManager.AddToRoleAsync(user, "User");
            
            return Ok(new { Message = "User registered successfully", UserId = user.Id });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return BadRequest(ModelState);
    }

    /// <summary>
    /// Handles the password flow for OpenIddict token endpoint
    /// </summary>
    [HttpPost("~/connect/token"), IgnoreAntiforgeryToken, Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        
        if (request == null)
        {
            return BadRequest("Invalid request");
        }

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordFlow(request);
        }
        
        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshTokenFlow(request);
        }

        return BadRequest("Unsupported grant type");
    }

    /// <summary>
    /// Handles OpenID Connect userinfo endpoint
    /// </summary>
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo"), HttpPost("~/connect/userinfo"), Produces("application/json")]
    public async Task<IActionResult> Userinfo()
    {
        var claimsPrincipal = HttpContext.User;
        var userId = claimsPrincipal.GetClaim(Claims.Subject);
        
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("Invalid user");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return BadRequest("User not found");
        }

        var userRoles = await _userManager.GetRolesAsync(user);

        var claims = new Dictionary<string, object>
        {
            [Claims.Subject] = user.Id.ToString(),
            [Claims.Email] = user.Email ?? string.Empty,
            [Claims.Name] = user.UserName ?? string.Empty,
            [Claims.PreferredUsername] = user.UserName ?? string.Empty,
            ["roles"] = userRoles
        };

        if (user.Profile != null)
        {
            claims[Claims.GivenName] = user.FirstName ?? string.Empty;
            claims[Claims.FamilyName] = user.LastName ?? string.Empty;
        }

        return Ok(claims);
    }

    /// <summary>
    /// Direct login endpoint for password authentication
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Unauthorized(new { Message = "Invalid email or password" });
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("User {Email} is locked out", request.Email);
            return Unauthorized(new { Message = "Account is locked out" });
        }

        // Create claims identity for OpenIddict
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(Claims.Subject, user.Id.ToString()));
        identity.AddClaim(new Claim(Claims.Email, user.Email ?? string.Empty));
        identity.AddClaim(new Claim(Claims.Name, user.UserName ?? string.Empty));

        // Add roles as claims
        var roles = await _userManager.GetRolesAsync(user);
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

        _logger.LogInformation("User {Email} logged in successfully", request.Email);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Revokes a refresh token
    /// </summary>
    [HttpPost("~/connect/revoke")]
    public async Task<IActionResult> Revoke()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
        {
            return BadRequest("Invalid request");
        }

        // Implement token revocation logic here
        return Ok();
    }

    private async Task<IActionResult> HandlePasswordFlow(OpenIddictRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.Username!);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password!))
        {
            var properties = new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid username or password."
            });

            return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            var properties = new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Account is locked out."
            });

            return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // Create the claims identity
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(Claims.Subject, user.Id.ToString()));
        identity.AddClaim(new Claim(Claims.Email, user.Email ?? string.Empty));
        identity.AddClaim(new Claim(Claims.Name, user.UserName ?? string.Empty));
        identity.AddClaim(new Claim(Claims.PreferredUsername, user.UserName ?? string.Empty));

        // Add roles
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(Claims.Role, role));
        }

        var principal = new ClaimsPrincipal(identity);

        // Set scopes
        principal.SetScopes(request.GetScopes());

        // Set resources
        principal.SetResources("electra-api");

        // Allow all requested destinations
        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        _logger.LogInformation("Password flow successful for user {Username}", request.Username);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleRefreshTokenFlow(OpenIddictRequest request)
    {
        // Retrieve the claims principal stored in the refresh token
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        
        if (!result.Succeeded)
        {
            var properties = new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid refresh token."
            });

            return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var userId = result.Principal.GetClaim(Claims.Subject);
        var user = await _userManager.FindByIdAsync(userId!);
        
        if (user == null)
        {
            var properties = new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "User no longer exists."
            });

            return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // Ensure the user is still allowed to sign in
        if (!await _signInManager.CanSignInAsync(user))
        {
            var properties = new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "User is no longer allowed to sign in."
            });

            return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // Create a new claims identity with updated claims
        var identity = new ClaimsIdentity(result.Principal.Claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        
        // Update roles in case they changed
        var currentRoles = await _userManager.GetRolesAsync(user);
        var existingRoleClaims = identity.Claims.Where(c => c.Type == Claims.Role).ToList();
        
        // Remove old role claims
        foreach (var claim in existingRoleClaims)
        {
            identity.RemoveClaim(claim);
        }
        
        // Add current roles
        foreach (var role in currentRoles)
        {
            identity.AddClaim(new Claim(Claims.Role, role));
        }

        var principal = new ClaimsPrincipal(identity);

        // Restore the scopes and resources
        principal.SetScopes(result.Principal.GetScopes());
        principal.SetResources(result.Principal.GetResources());

        // Allow all requested destinations
        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        _logger.LogInformation("Refresh token flow successful for user {UserId}", userId);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            case Claims.Name or Claims.PreferredUsername:
                yield return Destinations.AccessToken;
                if (principal.HasScope(Scopes.Profile))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;
                if (principal.HasScope(Scopes.Email))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;
                if (principal.HasScope("roles"))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Subject:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}