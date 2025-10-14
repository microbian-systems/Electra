using Electra.Models.Entities;
using Electra.Web.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Electra.Auth.Controllers;

public class AccountController(UserManager<ElectraUser> userManager, ILogger<AccountController> log) : ElectraWebBaseController(log)
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await HttpContext.SignOutAsync();
        return HandleReturnUrl(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reset(string? returnUrl)
    {
        foreach (var (key, _) in HttpContext.Request.Cookies)
        {
            if (!key.Contains("antiforgery", StringComparison.InvariantCultureIgnoreCase))
            {
                HttpContext.Response.Cookies.Delete(key);
            }
        }

        return HandleReturnUrl(returnUrl);
    }

    private RedirectToActionResult HandleReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            if (returnUrl == Url.Action("Index", "Passwordless"))
            {
                return RedirectToAction("Index", "Passwordless");
            }

            if (returnUrl == Url.Action("Index", "Usernameless"))
            {
                return RedirectToAction("Index", "Usernameless");
            }
        }

        return RedirectToAction("Index", "Home");
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

        var user = await userManager.FindByIdAsync(userId);
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

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found");
        }

        // This would require additional implementation to delete specific credentials
        // For now, return a placeholder response
        return Ok(new { Message = "Passkey deletion not yet implemented" });
    }
}
