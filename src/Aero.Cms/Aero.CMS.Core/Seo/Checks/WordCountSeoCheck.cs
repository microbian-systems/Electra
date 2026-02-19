using System.Linq;
using System.Threading.Tasks;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Seo.Interfaces;
using Aero.CMS.Core.Seo.Models;

namespace Aero.CMS.Core.Seo.Checks;

public class WordCountSeoCheck : ISeoCheck
{
    public string CheckAlias => "wordCount";
    public string DisplayName => "Word Count";

    public Task<SeoCheckResultItem> RunAsync(SeoCheckContext context)
    {
        var content = context.Content;
        var searchText = content.SearchText?.Trim();
        
        if (string.IsNullOrEmpty(searchText))
        {
            return Task.FromResult(new SeoCheckResultItem
            {
                CheckAlias = CheckAlias,
                DisplayName = DisplayName,
                Status = SeoCheckStatus.Fail,
                Message = "Content text is empty (SearchText not populated)"
            });
        }
        
        // Simple word count: split by whitespace and filter empty entries
        var words = searchText.Split(new[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        int wordCount = words.Length;
        
        if (wordCount >= 300)
        {
            return Task.FromResult(new SeoCheckResultItem
            {
                CheckAlias = CheckAlias,
                DisplayName = DisplayName,
                Status = SeoCheckStatus.Pass,
                Message = $"Word count ({wordCount}) meets recommended minimum of 300 words"
            });
        }
        else
        {
            return Task.FromResult(new SeoCheckResultItem
            {
                CheckAlias = CheckAlias,
                DisplayName = DisplayName,
                Status = SeoCheckStatus.Warning,
                Message = $"Word count ({wordCount}) is below recommended minimum of 300 words"
            });
        }
    }
}