using System;
using System.Linq;

namespace Aero.CMS.Core.Seo.Models;

public enum SeoCheckStatus
{
    Pass,
    Warning,
    Fail,
    Info
}

public class SeoCheckResultItem
{
    public required string CheckAlias { get; init; }
    public required string DisplayName { get; init; }
    public required SeoCheckStatus Status { get; init; }
    public string? Message { get; init; }
}

public class SeoCheckResult
{
    public List<SeoCheckResultItem> Items { get; } = [];
    
    public int Score
    {
        get
        {
            if (Items.Count == 0) return 0;
            
            int total = Items.Count * 100;
            int penalty = Items.Sum(item => item.Status switch
            {
                SeoCheckStatus.Pass => 0,
                SeoCheckStatus.Warning => 25,
                SeoCheckStatus.Fail => 100,
                SeoCheckStatus.Info => 0,
                _ => 0
            });
            
            return Math.Max(0, 100 - (penalty * 100 / total));
        }
    }
}