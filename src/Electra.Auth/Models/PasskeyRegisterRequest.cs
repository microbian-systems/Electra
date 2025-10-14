using System.ComponentModel.DataAnnotations;

namespace Electra.Auth.Models;

public class PasskeyRegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string DisplayName { get; set; } = string.Empty;
}

public class PasskeyAuthenticateRequest
{
    [Required]
    public string CredentialId { get; set; } = string.Empty;

    [Required]
    public string AuthenticatorData { get; set; } = string.Empty;

    [Required]
    public string ClientDataJson { get; set; } = string.Empty;

    [Required]
    public string Signature { get; set; } = string.Empty;
}