namespace Electra.Common.Web.Models;

public record ApiRegistrationRequest
{
    public string Email { get; set; } = string.Empty;
}