using Electra.Core.Entities;

namespace Electra.Models.Geo
{
    public class CountryModel : Entity<int>
    {
        [JsonPropertyName("fips")]
        public string FIPS { get; set; }
        [JsonPropertyName("iso")]
        public string ISO { get; set; }
        [JsonPropertyName("tld")]
        public string TLD { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}