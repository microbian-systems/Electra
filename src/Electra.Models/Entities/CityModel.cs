using Electra.Core.Entities;

namespace Electra.Models.Entities;

public class CityModel : Entity<int>
{
    [JsonPropertyName("fips")]
    public string FIPS { get; set; }    
    [JsonPropertyName("iso")]
    public string ISO { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
}