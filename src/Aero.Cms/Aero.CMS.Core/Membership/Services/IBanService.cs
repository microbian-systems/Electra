using System;
using System.Threading.Tasks;
using Aero.CMS.Core.Membership.Models;

namespace Aero.CMS.Core.Membership.Services;

public interface IBanService
{
    Task BanAsync(CmsUser user, string reason, DateTime? until = null);
    Task UnbanAsync(CmsUser user);
    Task<bool> IsBannedAsync(CmsUser user);
}
