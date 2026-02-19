using Aero.CMS.Core.Shared.Models;

namespace Aero.CMS.Core.Seo.Models;

public class SeoRedirectDocument : AuditableDocument
{
    public string? FromSlug { get; set; }
    public string? ToSlug { get; set; }
    public int StatusCode { get; set; } = 301;
    public bool IsActive { get; set; } = true;
}