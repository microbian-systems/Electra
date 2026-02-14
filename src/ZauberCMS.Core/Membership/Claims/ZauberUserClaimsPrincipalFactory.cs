using System.Security.Claims;
using Electra.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ZauberCMS.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ZauberCMS.Core.Membership.Models;

namespace ZauberCMS.Core.Membership.Claims;

public class ZauberUserClaimsPrincipalFactory(
    IServiceProvider serviceProvider,
    UserManager<ElectraUser> userManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ElectraUser>(userManager, optionsAccessor)
{
    private readonly UserManager<ElectraUser> _userManager = userManager;

    public override async Task<ClaimsPrincipal> CreateAsync(ElectraUser user)
    {
        using var scope = serviceProvider.CreateScope();
        var principal = await base.CreateAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        var claimsToAdd = new List<Claim> {new(Constants.Claims.Md5Hash, user.Email?.ToMd5() ?? string.Empty)};

        /*if (user.ProfileImage?.Url.IsNullOrWhiteSpace() == false)
        {
            claimsToAdd.Add(new Claim(Constants.Claims.ProfileImage, user.ProfileImage.Url));
        }*/

        if (roles.Count > 0)
        {
            foreach (var r in roles)
            {
                claimsToAdd.Add(new Claim(ClaimTypes.Role, r));
            }
        }

        if (claimsToAdd.Count > 0)
        {
            ((ClaimsIdentity)principal.Identity!).AddClaims(claimsToAdd.ToArray());
        }

        return principal;
    }
}