using System.Threading.Tasks;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Seo.Interfaces;
using Aero.CMS.Core.Seo.Models;

namespace Aero.CMS.Core.Seo.Checks;

public class MetaDescriptionSeoCheck : ISeoCheck
{
    public string CheckAlias => "metaDescription";
    public string DisplayName => "Meta Description";

    public Task<SeoCheckResultItem> RunAsync(SeoCheckContext context)
    {
        var content = context.Content;
        
        // Check if meta description exists in properties
        string? description = null;
        if (content.Properties != null && content.Properties.TryGetValue("metaDescription", out var descValue))
        {
            description = descValue?.ToString()?.Trim();
        }
        
        if (string.IsNullOrEmpty(description))
        {
            return Task.FromResult(new SeoCheckResultItem
            {
                CheckAlias = CheckAlias,
                DisplayName = DisplayName,
                Status = SeoCheckStatus.Fail,
                Message = "Meta description is missing"
            });
        }
        
        int length = description.Length;
        if (length >= 50 && length <= 160)
        {
            return Task.FromResult(new SeoCheckResultItem
            {
                CheckAlias = CheckAlias,
                DisplayName = DisplayName,
                Status = SeoCheckStatus.Pass,
                Message = $"Meta description length ({length}) is optimal (50-160 characters)"
            });
        }
        else
        {
            var status = SeoCheckStatus.Warning;
            var rangeMessage = length < 50 ? "too short" : "too long";
            return Task.FromResult(new SeoCheckResultItem
            {
                CheckAlias = CheckAlias,
                DisplayName = DisplayName,
                Status = status,
                Message = $"Meta description length ({length}) is {rangeMessage}. Aim for 50-160 characters."
            });
        }
    }
}