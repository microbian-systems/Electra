using Electra.Models.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using ZauberCMS.Core.Membership.Models;

namespace ZauberCMS.Core.Membership;

public sealed class IdentityUserAccessor(
    UserManager<ElectraUser> userManager,
    AuthenticationStateProvider authenticationStateProvider)
{
    public async Task<ElectraUser?> GetRequiredUserAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = await userManager.GetUserAsync(authState.User);
        return user;
    }
}