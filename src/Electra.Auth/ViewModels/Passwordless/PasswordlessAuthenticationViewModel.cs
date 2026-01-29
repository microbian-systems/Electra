using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Electra.Auth.Extensions;
using WebAuthn.Net.Models.Protocol.Enums;
using WebAuthn.Net.Services.AuthenticationCeremony.Models.CreateOptions;

namespace Electra.Auth.ViewModels.Passwordless;

[method: JsonConstructor]
public class PasswordlessAuthenticationViewModel(
    string userName,
    Dictionary<string, JsonElement>? extensions,
    string attestation,
    string userVerification)
{
    [JsonPropertyName("username")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [Required]
    public string UserName { get; } = userName;

    [JsonPropertyName("attestation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [Required]
    public string Attestation { get; } = attestation;

    [JsonPropertyName("userVerification")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [Required]
    public string UserVerification { get; } = userVerification;

    [JsonPropertyName("extensions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, JsonElement>? Extensions { get; } = extensions;
}
