namespace Electra.Services;

// todo - add some more properties
public sealed class JwtToken
{
    private readonly JwtSecurityToken token;

    public JwtToken(JwtSecurityToken token)
    {
        this.token = token;
    }

    public DateTime ValidTo => token.ValidTo;
    public string Value => new JwtSecurityTokenHandler().WriteToken(this.token);
}