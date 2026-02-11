using Microsoft.AspNetCore.Identity;

namespace Electra.Persistence.RavenDB.Identity;

/// <summary>
/// A authorization token created by a login provider.
/// </summary>
public class IdentityUserAuthToken : IdentityUserToken<string>
{
}