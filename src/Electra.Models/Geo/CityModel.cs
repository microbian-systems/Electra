using System.Text.Json.Serialization;
using Electra.Core.Entities;

namespace Electra.Models.Geo
{
    public record CityModel : Entity<int>
    {
        [JsonPropertyName("fips")]
        public string FIPS { get; set; }    
        [JsonPropertyName("iso")]
        public string ISO { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}