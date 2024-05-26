
using System.Text.RegularExpressions;

namespace Electra.Common.Web.Middleware;
/*
 * User-Agent strings can vary greatly depending on the browser version, operating system, and device. Here are some examples of User-Agent strings for the latest major desktop and mobile browsers as of 2022:

Desktop Browsers:
- Google Chrome: `Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36`
- Mozilla Firefox: `Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:97.0) Gecko/20100101 Firefox/97.0`
- Safari: `Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.3 Safari/605.1.15`
- Microsoft Edge: `Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Safari/537.36 Edg/98.0.1108.62`
- Opera: `Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Safari/537.36 OPR/82.0.4227.5s1`
Mobile Browsers:
- Google Chrome on Android: `Mozilla/5.0 (Linux; Android 12; Pixel 5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Mobile Safari/537.36`
- Safari on iPhone: `Mozilla/5.0 (iPhone; CPU iPhone OS 15_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.3 Mobile/15E148 Safari/604.1`
- Firefox on Android: `Mozilla/5.0 (Android 12; Mobile; rv:97.0) Gecko/97.0 Firefox/97.0`
- Samsung Internet on Samsung device: `Mozilla/5.0 (Linux; Android 12; SAMSUNG SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/16.0 Chrome/98.0.4758.102 Mobile Safari/537.36`

Please note that these are just examples and the actual User-Agent string can vary depending on the specific version of the browser and operating system.
 */
public partial class BrowserCompatibilityVersionMiddleware(
    RequestDelegate next,
    ILogger<BrowserCompatibilityVersionMiddleware> log)
{
    private static readonly Dictionary<string, string> LatestBrowserVersions = new Dictionary<string, string>
    {
        { "Chrome", "122.0.0.0" },
        { "Firefox", "112.0" },
        { "Safari", "16.4" },
        { "Edge", "113.0.1774.35" },
        { "Opera", "95.0.4652.30" },
        { "Mobile Safari", "16.3.1" },
        { "Chrome Mobile", "113.0.5672.63" },
        { "Firefox Mobile", "112.0" },
        { "Opera Mobile", "72.0.3815.186" },
    };

    public static readonly List<Regex> regexes  =
    [
        MyRegex(),
        new Regex(@"(?<browser>Firefox)/(?<version>\d+\.\d+)", RegexOptions.Compiled),
        new Regex(@"(?<browser>Safari)/(?<version>\d+\.\d+)", RegexOptions.Compiled),
        new Regex(@"(?<browser>Edge)/(?<version>\d+\.\d+\.\d+\.\d+)", RegexOptions.Compiled),
        new Regex(@"(?<browser>OPR)/(?<version>\d+\.\d+\.\d+\.\d+)", RegexOptions.Compiled),

        // Mobile browsers
        new Regex(@"(?<browser>Mobile Safari)/(?<version>\d+\.\d+\.\d+)", RegexOptions.Compiled),
        new Regex(@"(?<browser>Chrome)/(?<version>\d+\.\d+\.\d+\.\d+) Mobile", RegexOptions.Compiled),
        new Regex(@"(?<browser>Firefox)/(?<version>\d+\.\d+) Mobile", RegexOptions.Compiled),
        new Regex(@"(?<browser>OPR)/(?<version>\d+\.\d+\.\d+\.\d+) Mobile", RegexOptions.Compiled)
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var userAgent = context.Request.Headers["User-Agent"]  ;
        var browserInfo = GetBrowserInfo(userAgent);

        if (browserInfo != null)
        {
            string latestVersion;
            if (LatestBrowserVersions.TryGetValue(browserInfo.Value.BrowserName, out latestVersion))
            {
                var isUpToDate = browserInfo.Value.BrowserVersion == latestVersion;
                // Use the browserInfo and isUpToDate values as needed
                // For example, you could log the information or set a custom response header
            }
        }

        await next(context);
    }

    private (string BrowserName, string BrowserVersion)? GetBrowserInfo(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;

        foreach (var regex in regexes)
        {
            var match = regex.Match(userAgent);
            if (!match.Success) continue;

            var browserName = match.Groups["browser"].Value;
            var browserVersion = match.Groups["version"].Value;
            return (browserName, browserVersion);
        }

        return null;
    }

    [GeneratedRegex(@"(?<browser>Chrome)/(?<version>\d+\.\d+\.\d+\.\d+)", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}

public static class BrowserCompatibilityVersionMiddlewareExtensions
{
    public static IApplicationBuilder UseBrowserCompatibilityVersionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<BrowserCompatibilityVersionMiddleware>();
    }
}