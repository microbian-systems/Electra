using System.Text.Json.Serialization;

namespace Microbians.Models
{
    public class RefreshTokenRequest
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
        
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }
}