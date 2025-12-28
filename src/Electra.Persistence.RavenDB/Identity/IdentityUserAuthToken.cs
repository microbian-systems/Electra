using Microsoft.AspNetCore.Identity;

namespace Electra.Persistence.RavenDB;

/// <summary>
/// A authorization token created by a login provider.
/// </summary>
public class IdentityUserAuthToken : IdentityUserToken<string>
{
}