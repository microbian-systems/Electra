using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using ZauberCMS.Core.Membership.Models;

namespace ZauberCMS.Core.Membership;

public sealed class IdentityUserAccessor(
    UserManager<CmsUser> userManager,
    AuthenticationStateProvider authenticationStateProvider)
{
    public async Task<CmsUser?> GetRequiredUserAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = await userManager.GetUserAsync(authState.User);
        return user;
    }
}