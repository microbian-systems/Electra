using Aero.CMS.Core.Shared.Models;

namespace Aero.CMS.Core.Site.Models;

public class SiteDocument : AuditableDocument
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string DefaultLayout { get; set; } = "PublicLayout";
    public string? Description { get; set; }
    public string? FaviconMediaId { get; set; }
    public string? LogoMediaId { get; set; }
    public string? FooterText { get; set; }
    public bool IsDefault { get; set; } = true;
}
