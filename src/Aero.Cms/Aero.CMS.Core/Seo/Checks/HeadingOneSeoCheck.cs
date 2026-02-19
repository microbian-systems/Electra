using System.Linq;
using System.Threading.Tasks;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Seo.Interfaces;
using Aero.CMS.Core.Seo.Models;

namespace Aero.CMS.Core.Seo.Checks;

public class HeadingOneSeoCheck : ISeoCheck
{
    public string CheckAlias => "headingOne";
    public string DisplayName => "Heading One (H1)";

    public Task<SeoCheckResultItem> RunAsync(SeoCheckContext context)
    {
        var renderedHtml = context.RenderedHtml;
        
        if (string.IsNullOrEmpty(renderedHtml))
        {
            return Task.FromResult(new SeoCheckResultItem
            {
                CheckAlias = CheckAlias,
                DisplayName = DisplayName,
                Status = SeoCheckStatus.Info,
                Message = "Rendered HTML not available for H1 check"
            });
        }
        
        // Simple H1 tag counting (case-insensitive)
        // Count opening <h1> tags, ignoring self-closing and assuming proper HTML
        int h1Count = CountSubstring(renderedHtml, "<h1");
        
        if (h1Count == 0)
        {
            return Task.FromResult(new SeoCheckResultItem
            {
                CheckAlias = CheckAlias,
                DisplayName = DisplayName,
                Status = SeoCheckStatus.Fail,
                Message = "No H1 heading found"
            });
        }
        else if (h1Count == 1)
        {
            return Task.FromResult(new SeoCheckResultItem
            {
                CheckAlias = CheckAlias,
                DisplayName = DisplayName,
                Status = SeoCheckStatus.Pass,
                Message = "Exactly one H1 heading found (optimal)"
            });
        }
        else
        {
            return Task.FromResult(new SeoCheckResultItem
            {
                CheckAlias = CheckAlias,
                DisplayName = DisplayName,
                Status = SeoCheckStatus.Warning,
                Message = $"Multiple H1 headings found ({h1Count}). Aim for exactly one."
            });
        }
    }
    
    private static int CountSubstring(string text, string substring)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(substring, index, System.StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }
}