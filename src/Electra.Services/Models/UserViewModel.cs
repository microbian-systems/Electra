namespace Electra.Services.Models
{
    public record UserViewModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("firstname")]
        public string FirstName { get; set; }
        
        [JsonPropertyName("lastname")]
        public string LastName { get; set; }
        
        [JsonPropertyName("username")]
        public string Username { get; set; }
        
        [JsonPropertyName("email")]
        public string Email { get; set; }

        // [JsonPropertyName("password")] 
        // public string Password { get; set; } = null;
        
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("roles")] 
        public List<string> Roles { get; } = new();

        [JsonPropertyName("claims")]
        public List<Claim> Claims { get; } = new();
    }
}