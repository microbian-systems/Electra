using System.Text.Json.Serialization;
using WebAuthn.Net.Services.AuthenticationCeremony.Models.CreateOptions;

namespace Electra.Auth.ViewModels.Usernameless;

[method: JsonConstructor]
public class UsernamelessAuthenticationViewModel(Dictionary<string, JsonElement>? extensions)
{
    [JsonPropertyName("extensions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, JsonElement>? Extensions { get; } = extensions;

    public BeginAuthenticationCeremonyRequest ToBeginCeremonyRequest()
    {

        throw new NotImplementedException();

        //return new(
        //    null,
        //    null,
        //    null,
        //    32,
        //    120000,
        //    AuthenticationCeremonyIncludeCredentials.AllExisting(),
        //    null,
        //    null,
        //    null,
        //    null,
        //    Extensions);
    }
}
