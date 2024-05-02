using System.Text.Json.Serialization;

namespace Electra.Models
{
    public class RefreshTokenModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }
}