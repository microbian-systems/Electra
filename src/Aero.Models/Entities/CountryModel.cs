using System.ComponentModel.DataAnnotations;
using Aero.Core.Entities;

namespace Aero.Models.Entities;

public class CountryModel : Entity<int>
{
    [JsonPropertyName("fips")]
    [MaxLength(128)]
    public string FIPS { get; set; }
    [JsonPropertyName("iso")]
    [MaxLength(128)]
    public string ISO { get; set; }
    [JsonPropertyName("tld")]
    [MaxLength(128)]
    public string TLD { get; set; }
    [JsonPropertyName("name")]
    [MaxLength(128)]
    public string Name { get; set; }
}