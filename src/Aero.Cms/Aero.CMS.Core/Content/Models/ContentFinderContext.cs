using Microsoft.AspNetCore.Http;

namespace Aero.CMS.Core.Content.Models;

public class ContentFinderContext
{
    public required string Slug { get; init; }
    public required HttpContext HttpContext { get; init; }
    public string? LanguageCode { get; init; }
    public bool IsPreview { get; init; }
    public string? PreviewToken { get; init; }
}
