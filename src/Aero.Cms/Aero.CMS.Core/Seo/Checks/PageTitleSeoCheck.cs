using System.Threading.Tasks;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Seo.Interfaces;
using Aero.CMS.Core.Seo.Models;

namespace Aero.CMS.Core.Seo.Checks;

public class PageTitleSeoCheck : ISeoCheck
{
    public string CheckAlias => "pageTitle";
    public string DisplayName => "Page Title";

    public Task<SeoCheckResultItem> RunAsync(SeoCheckContext context)
    {
        var content = context.Content;
        var title = content.Name?.Trim();
        
        if (string.IsNullOrEmpty(title))
        {
            return Task.FromResult(new SeoCheckResultItem
            {
                CheckAlias = CheckAlias,
                DisplayName = DisplayName,
                Status = SeoCheckStatus.Fail,
                Message = "Page title is missing"
            });
        }
        
        int length = title.Length;
        if (length >= 10 && length <= 60)
        {
            return Task.FromResult(new SeoCheckResultItem
            {
                CheckAlias = CheckAlias,
                DisplayName = DisplayName,
                Status = SeoCheckStatus.Pass,
                Message = $"Page title length ({length}) is optimal (10-60 characters)"
            });
        }
        else
        {
            var status = length == 0 ? SeoCheckStatus.Fail : SeoCheckStatus.Warning;
            var rangeMessage = length < 10 ? "too short" : "too long";
            return Task.FromResult(new SeoCheckResultItem
            {
                CheckAlias = CheckAlias,
                DisplayName = DisplayName,
                Status = status,
                Message = $"Page title length ({length}) is {rangeMessage}. Aim for 10-60 characters."
            });
        }
    }
}