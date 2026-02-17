using Microsoft.AspNetCore.Identity;

namespace Aero.RavenDB.Identity;

/// <summary>
/// A authorization token created by a login provider.
/// </summary>
public class IdentityUserAuthToken : IdentityUserToken<string>
{
}