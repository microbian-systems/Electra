using System.Collections.Generic;

namespace Electra.Cloudflare;

public static class ExternalIpProviders
{
    public static IEnumerable<string> Providers { get; }

    static ExternalIpProviders()
    {
        Providers = new List<string>
        {
            "https://icanhazip.com/",
            "https://ipecho.net/plain",
            "https://whatismyip.akamai.com",
            "https://tnx.nl/ip"
        };
    }
}