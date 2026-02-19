using System.Threading.Tasks;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Seo.Models;

namespace Aero.CMS.Core.Seo.Interfaces;

public class SeoCheckContext
{
    public required ContentDocument Content { get; init; }
    public string? RenderedHtml { get; init; }
    public string? PublicUrl { get; init; }
}

public interface ISeoCheck
{
    string CheckAlias { get; }
    string DisplayName { get; }
    
    Task<SeoCheckResultItem> RunAsync(SeoCheckContext context);
}