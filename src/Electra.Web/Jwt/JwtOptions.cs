namespace Electra.Common.Web.Jwt;

public record JwtOptions
{
    public string Issuer { get; set; }
    public string Subject { get; set; }
    public string Audience { get; set; }
    public string Key { get; set; }
    public int ExpiryInMinutes { get; set; }
    public int RefreshExpiryInMinutes { get; set; }
    public string EncryptionKey { get; set; }
}

