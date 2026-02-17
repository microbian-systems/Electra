using System.ComponentModel.DataAnnotations;

namespace Aero.Auth.Models.ViewModels;

public class PasskeyViewModel
{
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public bool IsRegistration { get; set; }
}

public class PasskeyRegistrationViewModel
{
    [Required(ErrorMessage = "Passkey Name is required")]
    [Display(Name = "Passkey Name")]
    public string PasskeyName { get; set; } = string.Empty;
    
    [Display(Name = "Device Name")]
    public string? DeviceName { get; set; }
}
