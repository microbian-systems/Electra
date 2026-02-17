using System.Security.Cryptography;
using System.Text;
using Aero.Cms.Models;

namespace Aero.Cms.Services
{
    public static class ETagGenerator
    {
        public static string GenerateETag(SiteDocument site, PageDocument page)
        {
            var raw = $"site:{site.Id}:{site.Version}|page:{page.Id}:{page.Version}";
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return $"\"{System.Convert.ToHexString(hash)}\"";
        }
    }
}

