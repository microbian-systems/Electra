using Electra.Core.Entities;

namespace Electra.Services.Models;

public record UserViewModel : UserViewModel<string>;

public record UserViewModel<TKey> : IEntity<TKey>
    where TKey : IEquatable<TKey> , IComparable<TKey>
{
    // [JsonPropertyName("id")]
    // public TKey Id { get; set; }

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
    public List<string> Roles { get; } = [];

    [JsonPropertyName("claims")]
    public List<Claim> Claims { get; } = [];

    public TKey Id { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; }
    public string ModifiedBy { get; set; }
}