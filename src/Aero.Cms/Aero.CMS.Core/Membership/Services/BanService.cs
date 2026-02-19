using System;
using System.Threading.Tasks;
using Aero.CMS.Core.Membership.Models;

namespace Aero.CMS.Core.Membership.Services;

public class BanService : IBanService
{
    public Task BanAsync(CmsUser user, string reason, DateTime? until = null)
    {
        user.IsBanned = true;
        user.BanReason = reason;
        user.BannedUntil = until;
        return Task.CompletedTask;
    }

    public Task UnbanAsync(CmsUser user)
    {
        user.IsBanned = false;
        user.BanReason = null;
        user.BannedUntil = null;
        return Task.CompletedTask;
    }

    public Task<bool> IsBannedAsync(CmsUser user)
    {
        if (!user.IsBanned) return Task.FromResult(false);
        
        if (user.BannedUntil == null) return Task.FromResult(true);
        
        return Task.FromResult(user.BannedUntil > DateTime.UtcNow);
    }
}
