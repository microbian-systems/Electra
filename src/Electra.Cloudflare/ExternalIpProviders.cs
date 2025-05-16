using System.Collections.Generic;

namespace Electra.Cloudflare;

public static class ExternalIpProviders
{
    public static IEnumerable<string> Providers { get; }

    static ExternalIpProviders()
    {
        Providers = new List<string>
        {
            "https://ipecho.net/plain",
            "https://icanhazip.com/",
            "https://whatismyip.akamai.com",
            "https://tnx.nl/ip"
        };
    }
}