using NetTools;

// https://gist.github.com/tpeczek/f3f341df637e46ea7d077e017ea309e1
// todo - get cloudflare IPs from their api (vs hard coded)
namespace Aero.Common.Web.Middleware;

public static class CloudFlareConnectingIpExtensions
{
    public static IApplicationBuilder UseCloudFlareConnectingIp(this IApplicationBuilder app)
    {
        return app.UseCloudFlareConnectingIp(false);
    }

    public static IApplicationBuilder UseCloudFlareConnectingIp(this IApplicationBuilder app,
        bool checkOriginatesFromCloudflare)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        if (checkOriginatesFromCloudflare)
        {
            app.UseMiddleware<CloudFlareConnectingIpMiddleware>();
        }
        else
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedForHeaderName = CloudFlareConnectingIpMiddleware.CLOUDFLARE_CONNECTING_IP_HEADER_NAME,
                ForwardedHeaders = ForwardedHeaders.XForwardedFor
            });
        }

        return app;
    }
}

public class CloudFlareConnectingIpMiddleware
{
    public const string CLOUDFLARE_CONNECTING_IP_HEADER_NAME = "CF_CONNECTING_IP";

    private static readonly IPAddressRange[] _cloudFlareIpAddressRanges =
    [
        IPAddressRange.Parse("103.21.244.0/22"),
        IPAddressRange.Parse("103.22.200.0/22"),
        IPAddressRange.Parse("103.31.4.0/22"),
        IPAddressRange.Parse("104.16.0.0/12"),
        IPAddressRange.Parse("108.162.192.0/18"),
        IPAddressRange.Parse("131.0.72.0/22"),
        IPAddressRange.Parse("141.101.64.0/18"),
        IPAddressRange.Parse("162.158.0.0/15"),
        IPAddressRange.Parse("172.64.0.0/13"),
        IPAddressRange.Parse("173.245.48.0/20"),
        IPAddressRange.Parse("188.114.96.0/20"),
        IPAddressRange.Parse("190.93.240.0/20"),
        IPAddressRange.Parse("197.234.240.0/22"),
        IPAddressRange.Parse("198.41.128.0/17"),
        IPAddressRange.Parse("2400:cb00::/32"),
        IPAddressRange.Parse("2405:8100::/32"),
        IPAddressRange.Parse("2405:b500::/32"),
        IPAddressRange.Parse("2606:4700::/32"),
        IPAddressRange.Parse("2803:f800::/32"),
        IPAddressRange.Parse("2c0f:f248::/32"),
        IPAddressRange.Parse("2a06:98c0::/29")
    ];

    private readonly RequestDelegate _next;
    private readonly ForwardedHeadersMiddleware _forwardedHeadersMiddleware;

    public CloudFlareConnectingIpMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _forwardedHeadersMiddleware = new ForwardedHeadersMiddleware(next, loggerFactory,
            Microsoft.Extensions.Options.Options.Create(new ForwardedHeadersOptions
            {
                ForwardedForHeaderName = CLOUDFLARE_CONNECTING_IP_HEADER_NAME,
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
            }));
    }

    public Task Invoke(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey(CLOUDFLARE_CONNECTING_IP_HEADER_NAME) &&
            IsCloudFlareIp(context.Connection.RemoteIpAddress))
        {
            return _forwardedHeadersMiddleware.Invoke(context);
        }

        return _next(context);
    }

    private bool IsCloudFlareIp(IPAddress ipadress)
    {
        // for (int i = 0; i < _cloudFlareIpAddressRanges.Length; i++)
        // {
        //     isCloudFlareIp = _cloudFlareIpAddressRanges[i].Contains(ipadress);
        //     if (isCloudFlareIp)
        //     {
        //         break;
        //     }
        // }

        return _cloudFlareIpAddressRanges.Any(range => range.Contains(ipadress));
    }
}