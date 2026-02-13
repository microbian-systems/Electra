using Microsoft.AspNetCore.Identity;

namespace ZauberCMS.Core.Membership.Models;

public class UserRole : IdentityUserRole<string>
{
    public CmsRole Role { get; set; } = null!;
    public CmsUser User { get; set; } = null!;
}