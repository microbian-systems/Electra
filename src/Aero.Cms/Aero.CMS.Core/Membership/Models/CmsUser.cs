using System;
using System.Collections.Generic;
using Aero.CMS.Core.Shared.Models;

namespace Aero.CMS.Core.Membership.Models;

public class CmsUser : AuditableDocument
{
    public string UserName { get; set; } = string.Empty;
    public string? NormalizedUserName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PasswordHash { get; set; }
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    // Lockout
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }

    // Ban
    public bool IsBanned { get; set; }
    public DateTime? BannedUntil { get; set; }
    public string? BanReason { get; set; }

    // Collections
    public List<string> Roles { get; set; } = new();
    public List<UserClaim> Claims { get; set; } = new();
    public List<PasskeyCredential> Passkeys { get; set; } = new();
    public List<RefreshToken> RefreshTokens { get; set; } = new();
}
