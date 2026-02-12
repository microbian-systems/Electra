using System.Collections.Generic;

namespace Electra.Cms.Models
{
    public class SiteDocument
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Hostnames { get; set; } = new();
        public string DefaultCulture { get; set; }
        public string Theme { get; set; }
        public Dictionary<string, string> Settings { get; set; } = new();
        public int Version { get; set; }
    }
}
