using System.Linq;
using Electra.Auth.Models;
using Electra.Common.Web;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;

namespace Electra.Auth.Controllers;

[Route("api/auth")]
public class AuthorizationController(
    SignInManager<ElectraApplicationUser> signInManager,
    UserManager<ElectraApplicationUser> userManager,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictScopeManager scopeManager,
    ILogger<AuthorizationController> log)
    : ApiControllerBase(log)
{
    private readonly IOpenIddictApplicationManager applicationManager = applicationManager;
    private readonly IOpenIddictAuthorizationManager authorizationManager = authorizationManager;
    private readonly IOpenIddictScopeManager scopeManager = scopeManager;
    private readonly ILogger<AuthorizationController> log = log;

    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsPasswordGrantType())
        {
            var user = await userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                return Unauthorized(new OpenIddictResponse
                {
                    Error = Errors.InvalidGrant,
                    ErrorDescription = "The username/password couple is invalid."
                });
            }

            // Validate the username/password parameters and ensure the account is not locked out
            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                return Unauthorized(new OpenIddictResponse
                {
                    Error = Errors.InvalidGrant,
                    ErrorDescription = "The username/password couple is invalid."
                });
            }

            // Update last login timestamp
            user.LastLoginAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            // Create the claims-based identity that will be used by OpenIddict
            var identity = new ClaimsIdentity(
                TokenValidationParameters.DefaultAuthenticationType,
                Claims.Name,
                Claims.Role);

            // Add claims to the identity
            identity.AddClaim(Claims.Subject, await userManager.GetUserIdAsync(user));
            identity.AddClaim(Claims.Name, await userManager.GetUserNameAsync(user));
            identity.AddClaim(Claims.Email, await userManager.GetEmailAsync(user));

            // Add roles as claims
            foreach (var role in await userManager.GetRolesAsync(user))
            {
                identity.AddClaim(Claims.Role, role);
            }

            // Set the list of scopes granted to the client application
            identity.SetScopes(new[]
            {
                Scopes.OpenId,
                Scopes.Email,
                Scopes.Profile,
                Scopes.OfflineAccess,
                "api"
            }.Intersect(request.GetScopes()));

            identity.SetDestinations(GetDestinations);

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        else if (request.IsRefreshTokenGrantType())
        {
            // Retrieve the claims principal stored in the refresh token
            var principal =
                (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme))
                .Principal;

            // Retrieve the user specified by the principal
            var user = await userManager.FindByIdAsync(principal.GetClaim(Claims.Subject));
            if (user == null)
            {
                return Unauthorized(new OpenIddictResponse
                {
                    Error = Errors.InvalidGrant,
                    ErrorDescription = "The refresh token is no longer valid."
                });
            }

            // Ensure the user is still allowed to sign in
            if (!await signInManager.CanSignInAsync(user))
            {
                return Unauthorized(new OpenIddictResponse
                {
                    Error = Errors.InvalidGrant,
                    ErrorDescription = "The user is no longer allowed to sign in."
                });
            }

            // Create a new ClaimsIdentity containing the claims that
            // will be used to create an idtoken, a token or a code
            var identity = new ClaimsIdentity(
                TokenValidationParameters.DefaultAuthenticationType,
                Claims.Name,
                Claims.Role);

            // Add claims to the identity
            identity.AddClaim(Claims.Subject, await userManager.GetUserIdAsync(user));
            identity.AddClaim(Claims.Name, await userManager.GetUserNameAsync(user));
            identity.AddClaim(Claims.Email, await userManager.GetEmailAsync(user));

            // Add roles as claims
            foreach (var role in await userManager.GetRolesAsync(user))
            {
                identity.AddClaim(Claims.Role, role);
            }

            // Set the list of scopes granted to the client application
            identity.SetScopes(principal.GetScopes());
            identity.SetDestinations(GetDestinations);

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.UnsupportedGrantType,
            ErrorDescription = "The specified grant type is not supported."
        });
    }

    [HttpGet("~/connect/userinfo")]
    public async Task<IActionResult> Userinfo()
    {
        var claimsPrincipal =
            (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

        return Ok(new
        {
            Sub = claimsPrincipal.GetClaim(Claims.Subject),
            Name = claimsPrincipal.GetClaim(Claims.Name),
            Email = claimsPrincipal.GetClaim(Claims.Email),
            Roles = claimsPrincipal.GetClaims(Claims.Role)
        });
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

        switch (claim.Type)
        {
            case Claims.Name:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                break;

            case Claims.Email:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                break;

            case Claims.Role:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                break;

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp": yield break;

            default:
                yield return Destinations.AccessToken;
                break;
        }
    }
}