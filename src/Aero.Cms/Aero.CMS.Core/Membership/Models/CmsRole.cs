using System.Collections.Generic;
using Aero.CMS.Core.Shared.Models;

namespace Aero.CMS.Core.Membership.Models;

public class CmsRole : AuditableDocument
{
    public string Name { get; set; } = string.Empty;
    public string? NormalizedName { get; set; }
    public List<string> Permissions { get; set; } = new();
}
