using System.Text;

namespace Electra.Services;

public interface IJwtTokenBuilder
{
    JwtTokenBuilder AddSecurityKey();
    JwtTokenBuilder AddSecurityKey(string secret);
    JwtTokenBuilder AddSecurityKey(SecurityKey securityKey);
    JwtTokenBuilder AddSubject(string subject);
    JwtTokenBuilder AddIssuer(string issuer);
    JwtTokenBuilder AddAudience(string audience);
    JwtTokenBuilder AddClaim(string type, string value);
    JwtTokenBuilder AddClaims(Dictionary<string, string> claims);
    JwtTokenBuilder AddExpiry(int expiryInMinutes);
    JwtTokenBuilder AddExpiry(TimeSpan expiry);
    JwtToken Build();
}

public sealed class JwtTokenBuilder(IOptions<AppSettings> settings) : IJwtTokenBuilder
{
    private SecurityKey securityKey = default;
    private string subject = "";
    private string issuer = string.IsNullOrEmpty(settings.Value.ValidIssuers[0]) switch
    {
        true => throw new ArgumentNullException(nameof(AppSettings.ValidIssuers)),
        false => settings.Value.ValidIssuers[0]
    };
    private string audience = "";
    private Dictionary<string, string> claims = [];
    private TimeSpan expiry = TimeSpan.FromMinutes(15);

    public JwtTokenBuilder AddSecurityKey() => AddSecurityKey(settings.Value.Secret);

    public JwtTokenBuilder AddSecurityKey(string secret)
    {
        ArgumentException.ThrowIfNullOrEmpty(secret);
        if(secret.Length < 32)
            throw new ArgumentOutOfRangeException(nameof(secret), "Secret must be at least 32 characters long.");
        securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        return this;
    }

    public JwtTokenBuilder AddSecurityKey(SecurityKey securityKey)
    {
        this.securityKey = securityKey;
        return this;
    }

    public JwtTokenBuilder AddSubject(string subject)
    {
        ArgumentException.ThrowIfNullOrEmpty(subject);
        this.subject = subject;
        return this;
    }

    public JwtTokenBuilder AddIssuer(string issuer)
    {
        ArgumentException.ThrowIfNullOrEmpty(issuer);
        this.issuer = issuer;
        return this;
    }

    public JwtTokenBuilder AddAudience(string audience)
    {
        ArgumentException.ThrowIfNullOrEmpty(audience);
        this.audience = audience;
        return this;
    }

    public JwtTokenBuilder AddClaim(string type, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(type);
        ArgumentException.ThrowIfNullOrEmpty(value);
        claims.Add(type, value);
        return this;
    }

    public JwtTokenBuilder AddClaims(Dictionary<string, string> claims)
    {
        ArgumentNullException.ThrowIfNull(claims);
        this.claims = this.claims.Union(claims).ToDictionary();
        return this;
    }

    public JwtTokenBuilder AddExpiry(TimeSpan expiry) => AddExpiry((int)expiry.TotalMinutes);

    public JwtTokenBuilder AddExpiry(int expiry)
    {
        ArgumentOutOfRangeException
            .ThrowIfLessThan(expiry, 1, nameof(expiry));
        this.expiry = TimeSpan.FromMinutes(expiry);
        return this;
    }

    public JwtToken Build()
    {
        EnsureArguments();

        var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, subject),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }
            .Union(this.claims.Select(item => new Claim(item.Key, item.Value)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry.TotalMinutes),
            signingCredentials: new SigningCredentials(
                securityKey, SecurityAlgorithms.HmacSha256));

        var jwt = new JwtToken(token);
        return jwt;
    }

    private void EnsureArguments()
    {
        ArgumentNullException.ThrowIfNull(securityKey);
        ArgumentException.ThrowIfNullOrEmpty(subject);
        ArgumentException.ThrowIfNullOrEmpty(issuer);
        ArgumentException.ThrowIfNullOrEmpty(audience);
    }
}