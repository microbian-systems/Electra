using System.ComponentModel.DataAnnotations;

namespace ZauberCMS.Core.Membership.Parameters;

public class ForgotPasswordParameters
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
    public string? ReturnUrl { get; set; }
}