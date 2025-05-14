using Electra.Auth.Models;
using Electra.Common.Web;
using Microsoft.AspNetCore.Mvc;

namespace Electra.Auth.Controllers;

[Route("api/auth/[action]")]
public class RegisterController(
    UserManager<ElectraApplicationUser> userManager,
    ILogger<RegisterController> logger)
    : ApiControllerBase(logger)
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = new ElectraApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(ModelState);
        }

        // Assign the user to the default role
        await userManager.AddToRoleAsync(user, "User");

        logger.LogInformation("User {Email} created a new account with password", model.Email);

        return Ok(new
        {
            message = "User registered successfully"
        });
    }
}