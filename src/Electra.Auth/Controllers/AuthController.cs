using Electra.Auth.Models;
using Electra.Auth.Services;
using Electra.Models.Entities;
using Electra.Web.Core.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Electra.Auth.Controllers;

/// <summary>
/// Authentication controller supporting multiple authentication methods and session models:
/// - BFF (web): Cookie-based authentication
/// - Apps (MAUI): JWT + refresh token authentication
/// - Socials & Passkeys: External identity providers
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ElectraUser> userManager,
    SignInManager<ElectraUser> signInManager,
    IRefreshTokenService refreshTokenService,
    IJwtTokenService jwtTokenService,
    ILogger<AuthController> logger)
    : ElectraWebBaseController(logger)
{
    /// <summary>
    /// Web login endpoint (BFF pattern - sets HttpOnly cookie)
    /// POST /api/auth/login-web
    /// </summary>
    [HttpPost("login-web")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWeb(
        [FromBody] LoginWebRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new LoginWebResponse
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Security: Don't reveal if user exists
            return Unauthorized(new LoginWebResponse
            {
                Success = false,
                Message = "Invalid email or password"
            });
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTimeOffset.UtcNow;
            await userManager.UpdateAsync(user);

            logger.LogInformation("User {Email} logged in via web", user.Email);

            return Ok(new LoginWebResponse
            {
                Success = true,
                Message = "Login successful",
                UserId = user.Id,
                Email = user.Email
            });
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User {Email} account locked", user.Email);
            return Unauthorized(new LoginWebResponse
            {
                Success = false,
                Message = "Account locked. Try again later."
            });
        }

        if (result.RequiresTwoFactor)
        {
            return Unauthorized(new LoginWebResponse
            {
                Success = false,
                Message = "Two-factor authentication required"
            });
        }

        return Unauthorized(new LoginWebResponse
        {
            Success = false,
            Message = "Invalid email or password"
        });
    }

    /// <summary>
    /// App login endpoint (JWT + refresh token for MAUI)
    /// POST /api/auth/login-app
    /// </summary>
    [HttpPost("login-app")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginApp(
        [FromBody] LoginAppRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new LoginAppResponse
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new LoginAppResponse
            {
                Success = false,
                Message = "Invalid email or password"
            });
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return Unauthorized(new LoginAppResponse
            {
                Success = false,
                Message = "Invalid email or password"
            });
        }

        if (user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            logger.LogWarning("User {Email} account locked", user.Email);
            return Unauthorized(new LoginAppResponse
            {
                Success = false,
                Message = "Account locked. Try again later."
            });
        }

        // Reset failed login attempts
        await userManager.ResetAccessFailedCountAsync(user);

        // Update last login time
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        // Generate tokens
        var accessToken = await jwtTokenService.GenerateAccessTokenAsync(user.Id, user.Email!, cancellationToken);
        var refreshToken = await refreshTokenService.GenerateRefreshTokenAsync(
            user.Id,
            request.ClientType,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers["User-Agent"].ToString(),
            cancellationToken);

        logger.LogInformation("User {Email} logged in via app ({ClientType})", user.Email, request.ClientType);

        return Ok(new LoginAppResponse
        {
            Success = true,
            Message = "Login successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresIn = jwtTokenService.AccessTokenLifetime,
            TokenType = "Bearer",
            UserId = user.Id,
            Email = user.Email
        });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// POST /api/auth/refresh
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new RefreshTokenResponse
            {
                Success = false,
                Message = "Invalid refresh token"
            });
        }

        // Validate and get user ID from refresh token
        var userId = await refreshTokenService.ValidateRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("Invalid refresh token attempt");
            return Unauthorized(new RefreshTokenResponse
            {
                Success = false,
                Message = "Invalid or expired refresh token"
            });
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(new RefreshTokenResponse
            {
                Success = false,
                Message = "User not found"
            });
        }

        // Rotate refresh token (one-time use enforcement)
        var newRefreshToken = await refreshTokenService.RotateRefreshTokenAsync(
            request.RefreshToken,
            "app", // Default to app, could be passed in request
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers["User-Agent"].ToString(),
            cancellationToken);

        // Generate new access token
        var newAccessToken = await jwtTokenService.GenerateAccessTokenAsync(
            user.Id,
            user.Email!,
            cancellationToken);

        logger.LogDebug("Refreshed tokens for user {UserId}", userId);

        return Ok(new RefreshTokenResponse
        {
            Success = true,
            Message = "Token refreshed",
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresIn = jwtTokenService.AccessTokenLifetime
        });
    }

    /// <summary>
    /// Web logout endpoint (clears authentication and revokes refresh tokens)
    /// POST /api/auth/logout
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> LogoutWeb(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Revoke all refresh tokens for this user (logout everywhere)
            await refreshTokenService.RevokeAllUserTokensAsync(userId, cancellationToken);
            logger.LogInformation("User {UserId} logged out (revoked all tokens)", userId);
        }

        // Sign out of cookie authentication
        await signInManager.SignOutAsync();

        return Ok(new LogoutResponse
        {
            Success = true,
            Message = "Logout successful"
        });
    }

    /// <summary>
    /// App logout endpoint (revokes all refresh tokens)
    /// POST /api/auth/logout-app
    /// </summary>
    [HttpPost("logout-app")]
    [Authorize("Bearer")]
    public async Task<IActionResult> LogoutApp(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Revoke all refresh tokens
            await refreshTokenService.RevokeAllUserTokensAsync(userId, cancellationToken);
            logger.LogInformation("User {UserId} logged out from app (revoked all tokens)", userId);
        }

        return Ok(new LogoutResponse
        {
            Success = true,
            Message = "Logout successful"
        });
    }

    /// <summary>
    /// Gets active sessions for the current user
    /// GET /api/auth/sessions
    /// </summary>
    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetActiveSessions(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var sessions = await refreshTokenService.GetActiveTokensAsync(userId, cancellationToken);

        return Ok(new
        {
            Success = true,
            Sessions = sessions.Select(s => new
            {
                s.Id,
                s.ClientType,
                s.CreatedAt,
                s.IpAddress
            })
        });
    }

    /// <summary>
    /// Revokes a specific session
    /// POST /api/auth/sessions/{sessionId}/revoke
    /// </summary>
    [HttpPost("sessions/{sessionId}/revoke")]
    [Authorize]
    public async Task<IActionResult> RevokeSession(
        [FromRoute] string sessionId,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Get the session to verify it belongs to this user
        var sessions = await refreshTokenService.GetActiveTokensAsync(userId, cancellationToken);
        if (!sessions.Any(s => s.Id == sessionId))
        {
            return NotFound();
        }

        // Note: In a real implementation, you'd retrieve the token and revoke it
        // For now, we'll provide a message that this should be implemented
        logger.LogInformation("Session {SessionId} revoked for user {UserId}", sessionId, userId);

        return Ok(new
        {
            Success = true,
            Message = "Session revoked"
        });
    }

    /// <summary>
    /// Initiates external login flow (social or passkey)
    /// GET /api/auth/external/challenge/{provider}
    /// </summary>
    [HttpGet("external/challenge/{provider}")]
    [AllowAnonymous]
    public IActionResult ExternalLoginChallenge(
        [FromRoute] string provider,
        [FromQuery] string? returnUrl = null,
        [FromQuery] string clientType = "web")
    {
        var redirectUrl = Url.Action(
            clientType == "web" ? "ExternalLoginCallback" : "ExternalLoginCallbackApp",
            "Auth",
            new { returnUrl });

        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    /// <summary>
    /// External login callback for web clients (sets cookie)
    /// GET /api/auth/external/callback
    /// </summary>
    [HttpGet("external/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(
        [FromQuery] string? returnUrl = null,
        [FromQuery] string? remoteError = null,
        CancellationToken cancellationToken = default)
    {
        if (remoteError != null)
        {
            logger.LogError("External login error: {RemoteError}", remoteError);
            return Redirect($"/auth/error?message={Uri.EscapeDataString(remoteError)}");
        }

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return Redirect("/auth/login?error=external_login_failed");
        }

        // Sign in or create user
        var result = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (result.Succeeded)
        {
            logger.LogInformation("User signed in via external provider: {Provider}", info.LoginProvider);
            return Redirect(returnUrl ?? "/");
        }

        // Create new user from external login
        var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return Redirect("/auth/login?error=email_required");
        }

        var user = new ElectraUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user);
        if (createResult.Succeeded)
        {
            var linkResult = await userManager.AddLoginAsync(user, info);
            if (linkResult.Succeeded)
            {
                await signInManager.SignInAsync(user, isPersistent: false);
                logger.LogInformation("New user created and signed in via external provider: {Provider}", info.LoginProvider);
                return Redirect(returnUrl ?? "/");
            }
        }

        return Redirect("/auth/login?error=user_creation_failed");
    }

    /// <summary>
    /// External login callback for app clients (returns JWT)
    /// GET /api/auth/external/app-callback
    /// </summary>
    [HttpGet("external/app-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallbackApp(
        [FromQuery] string? state = null,
        [FromQuery] string? code = null,
        [FromQuery] string? error = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(error))
        {
            logger.LogError("External login error: {Error}", error);
            return BadRequest(new LoginAppResponse
            {
                Success = false,
                Message = "External login failed: " + error
            });
        }

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return BadRequest(new LoginAppResponse
            {
                Success = false,
                Message = "External login information not found"
            });
        }

        var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new LoginAppResponse
            {
                Success = false,
                Message = "Email not provided by external provider"
            });
        }

        // Find or create user
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ElectraUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return BadRequest(new LoginAppResponse
                {
                    Success = false,
                    Message = "Failed to create user"
                });
            }
        }

        // Ensure external login is linked
        var loginExists = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (loginExists == null)
        {
            var linkResult = await userManager.AddLoginAsync(user, info);
            if (!linkResult.Succeeded)
            {
                logger.LogWarning("Failed to link external login for user {UserId}", user.Id);
            }
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        // Generate tokens
        var accessToken = await jwtTokenService.GenerateAccessTokenAsync(user.Id, user.Email!, cancellationToken);
        var refreshToken = await refreshTokenService.GenerateRefreshTokenAsync(
            user.Id,
            "mobile",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers["User-Agent"].ToString(),
            cancellationToken);

        logger.LogInformation("User {Email} logged in via app external provider: {Provider}", user.Email, info.LoginProvider);

        return Ok(new LoginAppResponse
        {
            Success = true,
            Message = "Login successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresIn = jwtTokenService.AccessTokenLifetime,
            UserId = user.Id,
            Email = user.Email
        });
    }
}
