using Electra.Auth.Models;
using Electra.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using WebAuthn.Net.Services.AuthenticatorAssertionResponse;
using WebAuthn.Net.Services.AuthenticatorAttestationResponse;
using WebAuthn.Net.Services.Common.AuthenticatorAssertionResponse.Models.AuthenticatorAssertionResponse;
using WebAuthn.Net.Services.Common.AuthenticatorAttestationResponse.Models.AuthenticatorAttestationResponse;
using WebAuthn.Net.Services.PublicKeyCredentialCreationOptions;
using WebAuthn.Net.Services.PublicKeyCredentialRequestOptions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Electra.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasskeyController : ControllerBase
{
    private readonly UserManager<ElectraUser> _userManager;
    private readonly IPublicKeyCredentialCreationOptionsService _creationOptionsService;
    private readonly IPublicKeyCredentialRequestOptionsService _requestOptionsService;
    private readonly IAuthenticatorAttestationResponseService _attestationService;
    private readonly IAuthenticatorAssertionResponseService _assertionService;
    private readonly ILogger<PasskeyController> _logger;

    public PasskeyController(
        UserManager<ElectraUser> userManager,
        IPublicKeyCredentialCreationOptionsService creationOptionsService,
        IPublicKeyCredentialRequestOptionsService requestOptionsService,
        IAuthenticatorAttestationResponseService attestationService,
        IAuthenticatorAssertionResponseService assertionService,
        ILogger<PasskeyController> logger)
    {
        _userManager = userManager;
        _creationOptionsService = creationOptionsService;
        _requestOptionsService = requestOptionsService;
        _attestationService = attestationService;
        _assertionService = assertionService;
        _logger = logger;
    }

    /// <summary>
    /// Begins passkey registration process
    /// </summary>
    [HttpPost("register/begin")]
    public async Task<IActionResult> BeginRegister([FromBody] PasskeyRegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if user exists
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest("User not found. Please register first.");
        }

        try
        {
            var userId = Convert.ToBase64String(BitConverter.GetBytes(user.Id));
            
            var creationOptions = await _creationOptionsService.CreateAsync(
                HttpContext,
                userId,
                request.DisplayName,
                request.Email);

            return Ok(creationOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error beginning passkey registration for user {Email}", request.Email);
            return StatusCode(500, "Error beginning passkey registration");
        }
    }

    /// <summary>
    /// Completes passkey registration process
    /// </summary>
    [HttpPost("register/complete")]
    public async Task<IActionResult> CompleteRegister([FromBody] AuthenticatorAttestationResponseModel request)
    {
        try
        {
            var result = await _attestationService.VerifyAsync(HttpContext, request);
            
            if (result.HasError)
            {
                _logger.LogWarning("Passkey registration failed: {Error}", result.ErrorDescription);
                return BadRequest(result.ErrorDescription);
            }

            _logger.LogInformation("Passkey registered successfully for credential {CredentialId}", 
                Convert.ToBase64String(result.Ok.CredentialId));

            return Ok(new { Success = true, Message = "Passkey registered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing passkey registration");
            return StatusCode(500, "Error completing passkey registration");
        }
    }

    /// <summary>
    /// Begins passkey authentication process
    /// </summary>
    [HttpPost("authenticate/begin")]
    public async Task<IActionResult> BeginAuthenticate([FromBody] string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("Email is required");
        }

        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal if user exists or not
                return BadRequest("Authentication failed");
            }

            var userId = Convert.ToBase64String(BitConverter.GetBytes(user.Id));
            
            var requestOptions = await _requestOptionsService.CreateAsync(
                HttpContext,
                userId);

            return Ok(requestOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error beginning passkey authentication for email {Email}", email);
            return StatusCode(500, "Error beginning passkey authentication");
        }
    }

    /// <summary>
    /// Completes passkey authentication and returns JWT tokens
    /// </summary>
    [HttpPost("authenticate/complete")]
    public async Task<IActionResult> CompleteAuthenticate([FromBody] AuthenticatorAssertionResponseModel request)
    {
        try
        {
            var result = await _assertionService.VerifyAsync(HttpContext, request);
            
            if (result.HasError)
            {
                _logger.LogWarning("Passkey authentication failed: {Error}", result.ErrorDescription);
                return BadRequest(result.ErrorDescription);
            }

            // Get user ID from the verified credential
            var userIdBytes = result.Ok.UserHandle;
            if (userIdBytes == null || userIdBytes.Length != 8)
            {
                return BadRequest("Invalid user handle");
            }

            var userId = BitConverter.ToInt64(userIdBytes);
            var user = await _userManager.FindByIdAsync(userId.ToString());
            
            if (user == null)
            {
                return BadRequest("User not found");
            }

            // Create claims identity for OpenIddict
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(Claims.Subject, user.Id.ToString()));
            identity.AddClaim(new Claim(Claims.Email, user.Email ?? string.Empty));
            identity.AddClaim(new Claim(Claims.Name, user.UserName ?? string.Empty));
            identity.AddClaim(new Claim(Claims.AuthenticationMethod, "passkey"));

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

            _logger.LogInformation("User {Email} authenticated successfully via passkey", user.Email);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing passkey authentication");
            return StatusCode(500, "Error completing passkey authentication");
        }
    }

    /// <summary>
    /// Lists all passkeys for the current user
    /// </summary>
    [Authorize]
    [HttpGet("list")]
    public async Task<IActionResult> ListPasskeys()
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found");
        }

        // This would require additional implementation to store and retrieve passkey metadata
        // For now, return a placeholder response
        return Ok(new { Message = "Passkey listing not yet implemented" });
    }

    /// <summary>
    /// Deletes a specific passkey for the current user
    /// </summary>
    [Authorize]
    [HttpDelete("{credentialId}")]
    public async Task<IActionResult> DeletePasskey(string credentialId)
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found");
        }

        // This would require additional implementation to delete specific credentials
        // For now, return a placeholder response
        return Ok(new { Message = "Passkey deletion not yet implemented" });
    }
}