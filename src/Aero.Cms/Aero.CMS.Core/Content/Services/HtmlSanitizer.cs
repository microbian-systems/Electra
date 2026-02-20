using System.Text.RegularExpressions;

namespace Aero.CMS.Core.Content.Services;

public static class HtmlSanitizer
{
    private static readonly Regex ScriptTagRegex = new Regex(
        @"<script\b[^<]*(?:(?!</script>)<[^<]*)*</script>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex OnEventAttributeRegex = new Regex(
        @"\s+on\w+\s*=\s*(['""])(.*?)\1",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex JavascriptHrefRegex = new Regex(
        @"href\s*=\s*(['""])\s*javascript:\s*(.*?)\1",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex StyleTagRegex = new Regex(
        @"<style\b[^<]*(?:(?!</style>)<[^<]*)*</style>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex IframeTagRegex = new Regex(
        @"<iframe\b[^<]*(?:(?!</iframe>)<[^<]*)*</iframe>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    public static string Sanitize(string html)
    {
        if (string.IsNullOrEmpty(html))
            return html;
        
        var sanitized = html;
        
        // Remove script tags
        sanitized = ScriptTagRegex.Replace(sanitized, string.Empty);
        
        // Remove style tags (could contain malicious CSS)
        sanitized = StyleTagRegex.Replace(sanitized, string.Empty);
        
        // Remove iframe tags
        sanitized = IframeTagRegex.Replace(sanitized, string.Empty);
        
        // Remove on* event attributes
        sanitized = OnEventAttributeRegex.Replace(sanitized, string.Empty);
        
        // Remove javascript: href attributes
        sanitized = JavascriptHrefRegex.Replace(sanitized, "href=\"#\"");
        
        return sanitized;
    }
}